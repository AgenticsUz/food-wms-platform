using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Serilog;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Structured logging — konsol + kunlik aylanuvchi fayl (14 kun saqlanadi)
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/wms-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

// Database
builder.Services.AddDbContext<WmsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Health check (DB ulanishi) — /health, monitoring / load balancer uchun
builder.Services.AddHealthChecks().AddDbContextCheck<WmsDbContext>("database");

// JWT Auth
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// All three login flows (main, counterparty portal, agent portal) share one signing key,
// so token type is enforced by claim shape: portal tokens must never reach main endpoints.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MainApi", p => p.RequireClaim("userId"));
    options.AddPolicy("PortalOnly", p => p.RequireClaim("counterpartyId"));
    options.AddPolicy("AgentPortalOnly", p => p.RequireClaim("agentId"));
    // Platform egasi (control plane) — barcha tenantlarni boshqaradi
    options.AddPolicy("SuperAdmin", p => p.RequireClaim("isSuperAdmin", "true"));
});

// Services (DI)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICounterpartyService, CounterpartyService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<IQcService, QcService>();
builder.Services.AddScoped<IPortalAuthService, PortalAuthService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBatchExpiryService, BatchExpiryService>();
builder.Services.AddHostedService<BatchExpiryBackgroundService>();
builder.Services.AddHostedService<DbBackupBackgroundService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ITransferPdfService, TransferPdfService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Rate limiting — auth endpoint'larni IP bo'yicha cheklaymiz (register spam / brute-force'ga qarshi)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// CORS
builder.Services.AddHttpClient();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:7050"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// Har mutatsiyani avtomat audit qiladi (kim/nima/qachon)
builder.Services.AddControllers(options =>
{
    options.Filters.Add<WMS.API.Middleware.AuditLogFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
    db.Database.Migrate();
    await DataInitializer.SeedAsync(db, app.Configuration);
}

app.UseMiddleware<WMS.API.Middleware.ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

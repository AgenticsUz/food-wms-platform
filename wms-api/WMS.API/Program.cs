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

// wwwroot BUILDER'DAN OLDIN yaratiladi: ASP.NET static file'larni faqat papka ishga tushish
// paytida mavjud bo'lsa yoqadi. Yangi deploy'da papka birinchi logo yuklangunicha yo'q —
// natijada logolar keyingi restartgacha 404 bo'lardi.
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tenants"));

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

        // Tokenlar stateless, lekin parol tiklangach eskisi ishlamasligi kerak. Yechim:
        // foydalanuvchida SecurityStamp, tokenda uning nusxasi ("sstamp"). Parol o'zgarsa
        // stamp yangilanadi va eski token shu yerda rad etiladi (401).
        // Stamp'i yo'q foydalanuvchi — hech qachon tiklanmagan: tekshiruv o'tkazib yuboriladi,
        // shuning uchun bu o'zgarish hech kimni tizimdan chiqarib yubormaydi.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (principal == null) return;
                if (!int.TryParse(principal.FindFirst("userId")?.Value, out var userId) || userId <= 0)
                    return;   // portal tokenlari — ularda userId yo'q

                var security = context.HttpContext.RequestServices.GetRequiredService<IUserSecurityService>();
                var current = await security.GetStampAsync(userId, context.HttpContext.RequestAborted);
                if (string.IsNullOrEmpty(current)) return;

                if (principal.FindFirst("sstamp")?.Value != current)
                    context.Fail("Password changed — this session is no longer valid");
            }
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

// Subscription / obuna sozlamalari (trial uzunligi, grace, cache) — env orqali beriladi
builder.Services.Configure<WMS.Application.Common.SubscriptionOptions>(
    builder.Configuration.GetSection(WMS.Application.Common.SubscriptionOptions.SectionName));
builder.Services.AddMemoryCache();

// Til: har so'rovda Accept-Language bo'yicha aniqlanadi (uz / ru / en).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestLanguage, WMS.API.Middleware.RequestLanguage>();

// Services (DI)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<ITenantStateService, TenantStateService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IFeatureService, FeatureService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IBrandingService, BrandingService>();
builder.Services.AddScoped<IBrandingFileStore, WMS.API.Services.BrandingFileStore>();
// So'rov davomida yig'iladigan ogohlantirishlar (limit 80 % va h.k.) — javob filtri o'qiydi.
builder.Services.AddScoped<IRequestWarnings, RequestWarnings>();
builder.Services.AddScoped<IUserSecurityService, UserSecurityService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
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
builder.Services.AddScoped<ITelegramService, TelegramService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBatchExpiryService, BatchExpiryService>();
builder.Services.AddHostedService<BatchExpiryBackgroundService>();
builder.Services.AddHostedService<DbBackupBackgroundService>();
builder.Services.AddHostedService<SubscriptionExpiryBackgroundService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ITransferPdfService, TransferPdfService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IDeliveryPdfService, DeliveryPdfService>();
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

    // Login sahifasi brendlashni autentifikatsiyasiz so'raydi — arzon, lekin cheksiz emas.
    options.AddPolicy("public", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Demo so'rovi anonim va public — soatiga 5 ta (auth policy'dan ajratilgan, chunki
    // bu forma odam tomonidan kuniga bir marta to'ldiriladi, login esa tez-tez bo'ladi).
    options.AddPolicy("leads", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
});

// CORS
builder.Services.AddHttpClient();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:7050", "http://localhost:7060"];

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
    // Muvaffaqiyat xabarlarini so'ralgan tilga o'giradi ("Deleted" → "O'chirildi").
    options.Filters.Add<WMS.API.Middleware.ResponseLocalizationFilter>();
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
// Tenant logolari wwwroot'dan autentifikatsiyasiz beriladi — logo maxfiy emas va u
// login sahifasida, ya'ni token paydo bo'lishidan oldin kerak bo'ladi.
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// Obuna har so'rovda tekshiriladi (suspend/trial tugashi darhol kuchga kiradi) —
// autentifikatsiyadan keyin turishi shart.
app.UseMiddleware<WMS.API.Middleware.SubscriptionEnforcementMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

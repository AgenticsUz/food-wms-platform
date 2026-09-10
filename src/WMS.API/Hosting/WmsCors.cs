namespace WMS.API.Hosting;

/// <summary>
/// CORS — <c>Cors:AllowedOrigins</c>. Bo'sh ro'yxat → hech kimga ruxsat yo'q (fail-closed, CLAUDE.md 3-qoida).
/// </summary>
/// <remarks>
/// SQLite davrida ro'yxat berilmasa sukut <c>localhost:7050/7060</c> ga ochilardi va prod'da
/// o'sha qator jimgina qolardi. Prod'da wms-web API bilan BIR origin'da (nginx), ya'ni ro'yxat
/// odatda faqat dev uchun kerak.
/// </remarks>
public static class WmsCors
{
    public const string PolicyName = "wms-spa";

    private static readonly string[] AllowedRequestHeaders =
        ["Authorization", "Content-Type", "X-Correlation-Id", "X-Tenant-Id", "Accept-Language", "X-Requested-With"];

    private static readonly string[] ExposedResponseHeaders = ["X-Correlation-Id", "Content-Disposition"];

    private static readonly string[] AllowedMethods = ["GET", "HEAD", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];

    public static IServiceCollection AddWmsCors(this IServiceCollection services, IConfiguration configuration)
    {
        string[] origins = [.. (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        services.AddCors(cors => cors.AddPolicy(PolicyName, policy =>
        {
            if (origins.Length == 0)
            {
                return;
            }

            policy.WithOrigins(origins)
                .WithMethods(AllowedMethods)
                .WithHeaders(AllowedRequestHeaders)
                .WithExposedHeaders(ExposedResponseHeaders)
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        }));

        return services;
    }
}

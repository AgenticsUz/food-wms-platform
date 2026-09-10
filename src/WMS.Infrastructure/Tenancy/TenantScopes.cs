using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Infrastructure.Tenancy;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Tenancy;

/// <summary>
/// Fon vazifalari uchun: har faol tenant uchun ALOHIDA DI scope va o'sha tenantning konteksti (D12).
/// </summary>
/// <remarks>
/// <para>
/// SQLite davrida fon xizmatlari bitta kontekstda BARCHA tenantlarni <c>TenantId</c> bo'yicha
/// guruhlab yurardi. RLS ostida bu mumkin emas: kontekstsiz so'rov 0 qator beradi. Tenant jadvali
/// esa platforma jadvali (RLS yo'q) — ro'yxat shundan olinadi.
/// </para>
/// <para>
/// ⚠️ Har tenant O'Z scope'ida: bitta <c>DbContext</c> da tenant almashtirilsa kuzatuvchi
/// oldingi tenantning obyektlarini ushlab qolar va <c>StampEntries</c> ularni «begona tenant» deb
/// rad etardi. Bir tenantdagi nosozlik qolganlarini to'xtatmaydi — logga yoziladi.
/// </para>
/// </remarks>
public static class TenantScopes
{
    public static async Task ForEachTenantAsync(
        IServiceProvider root,
        Func<IServiceProvider, Guid, CancellationToken, Task> work,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(work);
        ArgumentNullException.ThrowIfNull(logger);

        List<(Guid Id, string Code)> tenants;
        await using (AsyncServiceScope listing = root.CreateAsyncScope())
        {
            WmsDbContext db = listing.ServiceProvider.GetRequiredService<WmsDbContext>();
            tenants = [.. (await db.Tenants.AsNoTracking()
                    .Where(t => t.IsActive)
                    .Select(t => new { t.Id, t.Code })
                    .ToListAsync(cancellationToken))
                .Select(t => (t.Id, t.Code))];
        }

        foreach ((Guid id, string code) in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using AsyncServiceScope scope = root.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(id, code);

            try
            {
                await work(scope.ServiceProvider, id, cancellationToken);
            }
#pragma warning disable CA1031 // Bitta tenantning nosozligi qolganlarini to'xtatmasin.
            catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Fon vazifasi tenant {TenantId} ({TenantCode}) uchun yiqildi", id, code);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Tenancy;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Seeding;

/// <summary>
/// Demo zavod: Identity demo tenantining WMS nusxasi, to'rt profil va 35 kunlik ish tarixi
/// (<c>seed demo</c> rejimi, PLATFORMA-TZ §7·F6 W2·6).
/// </summary>
/// <remarks>
/// <para>
/// SQLite bazasi KO'CHIRILMAYDI (HOLAT, S6) — mijoz yo'q; eski <c>DataInitializer.SeedDemoDataAsync</c>
/// to'plami shu yerda qayta tuzilgan (Guid v7 kalitlar, tenant id qo'lda berilmaydi —
/// <c>StampEntries</c> joriy tenantdan yozadi).
/// </para>
/// <para>
/// ⚠️ <b>Wash'dan farqi — tenant va profil OLDINDAN yoziladi.</b> Wash seederi JIT nusxani kutadi
/// va xodimni telefon bo'yicha topadi, chunki Wash sub'larni bila olmaydi. WMS demo odamlarining
/// sub'lari Identity demo seed'ida QOTIRILGAN (<c>f0000001-…-21..24</c>), ya'ni nusxani JIT
/// yozadigan shaklda (o'sha id, kod, modullar; profil — o'sha <c>identity_sub</c>) oldindan yozish
/// mumkin: JIT keyin ularni topadi va dublikat ochmaydi, rolga ham tegmaydi (profilda rol bor).
/// Foydasi: <c>seed demo</c> hech kim kirmasdan to'liq demo beradi — tarixdagi transfer, buyurtma,
/// to'lovlar aniq odamlarga bog'lanadi. Telefon esa zaxira kalit: sub eskirgan bo'lsa JIT
/// yozgan profil telefon bo'yicha topiladi.
/// </para>
/// <para>
/// Business servislarga ATAYLAB tayanmaydi — to'g'ridan-to'g'ri <see cref="WmsDbContext"/>: seed
/// servis API'si o'zgarganda (F6 da ular parallel ko'chirilmoqda) sinmasin. Ombor/qarz qoidalari
/// <see cref="DemoData"/> da servislardagidek takrorlangan va oxirida o'z-o'zini tekshiradi.
/// </para>
/// <para>
/// <b>Idempotentlik.</b> Tenant nusxasi, bazaviy rollar va profillar — har yurishda idempotent
/// (yetishmaganini qo'shadi). Tarix esa bir marta: belgi — «demo tenantda ombor bor» (o'chirilgan
/// omborlar ham hisobga olinadi). Ikkinchi yurish hech narsani o'zgartirmaydi.
/// </para>
/// </remarks>
public sealed class DemoSeeder
{
    private readonly WmsDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly TenantBaseline _baseline;
    private readonly ILogger<DemoSeeder> _logger;

    public DemoSeeder(WmsDbContext db, ICurrentTenant currentTenant, TenantBaseline baseline, ILogger<DemoSeeder> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _baseline = baseline;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        DemoManifest manifest = DemoManifest.Load();
        Guid tenantId = manifest.Tenant.Id;
        string tenantCode = manifest.Tenant.Code;

        // Tenant konteksti BIRINCHI baza so'rovidan oldin: `app.tenant_id` ulanish ochilgan lahzada
        // o'qiladi (TenantConnectionInterceptor) — kechikib qo'yilsa RLS 0 qator berardi.
        _currentTenant.Set(tenantId, tenantCode);

        await EnsureTenantCopyAsync(manifest, ct);

        // JIT yangi tenantga aynan shuni chaqiradi: to'rt tizim roli (ruxsatlari bilan) va birliklar.
        await _baseline.EnsureAsync(tenantId, tenantCode, ct);
        _currentTenant.Set(tenantId, tenantCode);

        DemoPeople people = await EnsureProfilesAsync(manifest, ct);

        // Soft-delete filtri ATAYLAB o'chiriladi: demo omborlarini o'chirib tajriba qilgan odam
        // keyingi `seed demo` da ikkinchi mahsulot katalogini olmasin.
        bool seeded = await _db.Warehouses.IgnoreQueryFilters([AppQueryFilters.SoftDelete]).AnyAsync(ct);
        if (seeded)
        {
            _logger.LogInformation("Demo tenant '{TenantCode}' da ma'lumot bor — tarix qayta yozilmadi", tenantCode);
            return;
        }

        // Bitta tranzaksiya: yarim yozilgan tarix belgini (ombor) qo'yib qo'ysa, keyingi yurishlar
        // uni hech qachon to'ldirmasdi.
        await using IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync(ct);
        IReadOnlyDictionary<string, int> summary = await new DemoData(_db, people, DateTime.UtcNow).WriteAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation(
            "Demo tenant '{TenantCode}' to'ldirildi: {Summary}",
            tenantCode,
            string.Join(", ", summary.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    /// <summary>Tenant nusxasi — JIT (<c>WmsPlatformUserSink</c>) yozadigan shaklda.</summary>
    private async Task EnsureTenantCopyAsync(DemoManifest manifest, CancellationToken ct)
    {
        DateTime now = DateTime.UtcNow;

        // Plan BaseCatalogSeeder'niki (migrate qadami). Yo'q bo'lsa — tartib buzilgan: jim trial
        // o'rniga aniq xato, aks holda demo 14 kunda «obuna tugadi» ekraniga tushardi.
        Plan plan = await _db.Plans.FirstOrDefaultAsync(p => p.Code == manifest.PlanCode && p.IsActive, ct)
            ?? throw new InvalidOperationException(
                $"'{manifest.PlanCode}' plani yo'q — BaseCatalogSeeder demo seed'dan OLDIN yurishi kerak.");
        SubscriptionStatus status = manifest.ParseSubscriptionStatus();

        Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == manifest.Tenant.Id, ct);

        if (tenant is null)
        {
            // Kod noyob. Boshqa id bilan band bo'lsa manifest Identity'dan ajralgan — ustiga yozish
            // begona tenantning ma'lumotini demo'ga aylantirardi.
            if (await _db.Tenants.AnyAsync(t => t.Code == manifest.Tenant.Code, ct))
            {
                throw new InvalidOperationException(
                    $"wms.tenant da '{manifest.Tenant.Code}' kodi boshqa id bilan band — seed/demo/tenant.json " +
                    "agentics-platform/seed/demo/01-identity.sql bilan ajralgan.");
            }

            tenant = Tenant.FromToken(manifest.Tenant.Id, manifest.Tenant.Code, manifest.Modules, now);
            tenant.Name = manifest.Tenant.Name;
            ApplyCommercialState(tenant, plan, status, now);

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Demo tenant nusxasi yozildi ({TenantId}, plan {Plan})", tenant.Id, plan.Code);
            return;
        }

        // Kimdir seed'dan oldin kirgan — nusxani JIT yozgan. JIT har tokenda qiladigan sinxron.
        bool changed = tenant.SyncFromToken(manifest.Tenant.Code, manifest.Modules, now);

        // JIT nomni kod bilan to'ldiradi (tokenda tenant nomi yo'q); Console qo'ygan nomga tegilmaydi.
        if (string.Equals(tenant.Name, tenant.Code, StringComparison.Ordinal)
            && !string.Equals(tenant.Name, manifest.Tenant.Name, StringComparison.Ordinal))
        {
            tenant.Name = manifest.Tenant.Name;
            changed = true;
        }

        // Tijorat holati faqat nusxa hali JIT'ning sukut shaklida bo'lsa (trial, demo plani emas):
        // Console operatori keyin o'zgartirgan plan/holatni har `seed demo` bosib ketmasin.
        if (tenant.SubscriptionStatus == SubscriptionStatus.Trial && tenant.PlanId != plan.Id)
        {
            ApplyCommercialState(tenant, plan, status, now);
            changed = true;
        }

        if (changed)
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <remarks>
    /// Sukut (Trial) plani emas: u 3 foydalanuvchi va 2 ombor bilan cheklangan (demo'da 4 odam) va
    /// 14 kunda tugaydi. <c>Active</c> + <c>PaidUntil = null</c> — <c>SubscriptionPolicy</c> bunday
    /// tenantni to'lov uchun HECH QACHON bloklamaydi, ya'ni demo stend muddatsiz ishlaydi.
    /// </remarks>
    private static void ApplyCommercialState(Tenant tenant, Plan plan, SubscriptionStatus status, DateTime now)
    {
        tenant.PlanId = plan.Id;
        tenant.SubscriptionStatus = status;
        tenant.TrialEndsAt = status == SubscriptionStatus.Trial ? now.AddDays(plan.TrialDays > 0 ? plan.TrialDays : 14) : null;
        tenant.PaidUntil = null;
    }

    /// <summary>To'rt profil + tizim rollari — JIT qoidasi bilan (rol faqat profil ROLSIZ bo'lsa).</summary>
    private async Task<DemoPeople> EnsureProfilesAsync(DemoManifest manifest, CancellationToken ct)
    {
        Dictionary<string, Guid> roleIds = await _db.Roles
            .Where(r => r.Code != null)
            .ToDictionaryAsync(r => r.Code!, r => r.Id, StringComparer.OrdinalIgnoreCase, ct);

        List<UserProfile> profiles = await _db.UserProfiles.Include(p => p.UserRoles).ToListAsync(ct);
        Dictionary<string, UserProfile> byRole = new(StringComparer.Ordinal);
        int created = 0;
        int granted = 0;

        foreach (DemoPersonSeed person in manifest.Users)
        {
            UserProfile? profile = profiles.Find(p => p.IdentitySub == person.Sub);

            if (profile is null)
            {
                // Sub topilmadi, lekin shu telefonli profil bor — Identity'dagi id manifestdagidan
                // farq qiladi (qayta seed qilingan baza). JIT yozgan profil haqiqiy: ikkinchisini
                // ochish demo tarixini «kirmaydigan» odamga bog'lardi.
                profile = profiles.Find(p => string.Equals(p.Phone, person.Phone, StringComparison.Ordinal));
                if (profile is not null)
                {
                    _logger.LogWarning(
                        "Demo odam {Phone}: sub {ManifestSub} o'rniga mavjud profil (sub {StoredSub}) ishlatildi — tenant.json Identity seed'idan ajralgan",
                        person.Phone, person.Sub, profile.IdentitySub);
                }
            }

            if (profile is null)
            {
                profile = new UserProfile
                {
                    IdentitySub = person.Sub,
                    FullName = person.FullName,
                    Phone = person.Phone,
                };
                _db.UserProfiles.Add(profile);
                profiles.Add(profile);
                created++;
            }

            if (profile.UserRoles.Count == 0)
            {
                Guid roleId = roleIds.TryGetValue(person.Role, out Guid id)
                    ? id
                    : throw new InvalidOperationException($"'{person.Role}' tizim roli yo'q — TenantBaseline uni yaratishi kerak edi.");

                UserRole link = new() { UserId = profile.Id, RoleId = roleId };
                _db.UserRoles.Add(link);
                profile.UserRoles.Add(link);
                granted++;
            }

            byRole[person.Role] = profile;
        }

        await _db.SaveChangesAsync(ct);

        if (created > 0 || granted > 0)
        {
            _logger.LogInformation("Demo profillar: {Created} ta yaratildi, {Granted} tasiga tizim roli berildi", created, granted);
        }

        return new DemoPeople(byRole["admin"], byRole["manager"], byRole["employee"], byRole["viewer"]);
    }
}

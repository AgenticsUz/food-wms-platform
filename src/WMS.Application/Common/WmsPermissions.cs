namespace WMS.Application.Common;

/// <summary>Ruxsat katalogidagi bitta yozuv.</summary>
/// <param name="Code">Kod (<c>warehouse.view</c>).</param>
/// <param name="Name">Nom (inglizcha — frontend tarjima qiladi).</param>
/// <param name="Group">Guruh (<c>WAREHOUSE</c>) — modul bilan 1:1 EMAS, <c>PermissionModules</c> ga qarang.</param>
public sealed record PermissionDefinition(string Code, string Name, string Group);

/// <summary>
/// WMS'ning nozik ruxsat katalogi — YAGONA manba.
/// </summary>
/// <remarks>
/// SQLite davrida katalog bazada (<c>Permission</c> jadvali, <c>HasData</c>) va
/// controller'larda satr literal sifatida ikki joyda yashardi. Endi kodda: u faqat
/// deploy bilan o'zgaradi, rol xaritasi (<c>WmsRoleMap</c>), Console'ning
/// <c>permission-catalog</c> yuzasi va rollar ekrani BITTA ro'yxatni ko'radi.
/// Kodlar o'zgarmadi — wms-web ularni o'sha shaklda tekshiradi.
/// </remarks>
public static class WmsPermissions
{
    public const string DashboardView = "dashboard.view";
    public const string WarehouseView = "warehouse.view";
    public const string WarehouseManage = "warehouse.manage";
    public const string TransfersView = "transfers.view";
    public const string TransfersCreate = "transfers.create";
    public const string TransfersConfirm = "transfers.confirm";
    public const string TransfersReject = "transfers.reject";
    public const string ProductionView = "production.view";
    public const string ProductionManage = "production.manage";
    public const string FinanceView = "finance.view";
    public const string FinanceManage = "finance.manage";
    public const string KpiView = "kpi.view";
    public const string KpiManage = "kpi.manage";
    public const string PartnersView = "partners.view";
    public const string PartnersManage = "partners.manage";
    public const string ProductsView = "products.view";
    public const string ProductsManage = "products.manage";
    public const string SettingsUsers = "settings.users";
    public const string SettingsRoles = "settings.roles";
    public const string SettingsModules = "settings.modules";
    public const string QualityView = "quality.view";
    public const string QualityManage = "quality.manage";
    public const string AgentsView = "agents.view";
    public const string AgentsManage = "agents.manage";
    public const string AuditView = "audit.view";
    public const string DeliveryView = "delivery.view";
    public const string DeliveryManage = "delivery.manage";

    /// <summary>Hujjat va to'lovni ORQAGA sana bilan kiritish.</summary>
    /// <remarks>
    /// Nega alohida ruxsat: orqaga sana — hisobotni QAYTA yozish imkoni (kechagi kun
    /// yopilgandan keyin unga yangi hujjat qo'shish). Har kim uchun ochiq bo'lsa
    /// «kecha»gi raqamlar hech qachon barqaror bo'lmaydi. Hujjat ham, to'lov ham BITTA
    /// ruxsatga bog'lanadi — ikkita deyarli bir xil ruxsat faqat chalkashtirardi.
    /// Kodda tekshiruv SANAGA qarab ishlaydi (yuk tarkibiga bog'liq entitlement
    /// naqshi — `EnsureTransferTypeAllowedAsync` bilan bir xil).
    /// </remarks>
    public const string DocumentsBackdate = "documents.backdate";

    /// <summary>
    /// Kabinet foydalanuvchisining O'Z ma'lumoti (F10·A2 AI tool'lari uchun).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Bu kod <see cref="All"/> katalogida ATAYLAB YO'Q</b> va hech bir rolga
    /// berilmaydi: uni faqat kabinet yuzasi (<c>/api/portal/ai/chat</c>) so'rov davomida
    /// beradi. Sabab — kabinet rollarining (<c>client</c>, <c>agent</c>) WMS ruxsati bo'sh
    /// (<c>PortalController</c> izohi), ya'ni tool registri ularga hech narsa ko'rsatmasdi.
    /// Katalogga qo'shilsa esa u rollar ekranida paydo bo'lib, xodimga ham berilishi mumkin
    /// bo'lardi — va o'shanda «o'zining» ma'lumoti kimniki ekani noaniq bo'lardi.
    /// Himoya baribir ikki qatlamli: tool'lar <c>IPortalService</c> ni chaqiradi va u
    /// tokendagi <c>sub</c> ni kartaga bog'lay olmasa 403 beradi (fail-closed).
    /// </remarks>
    public const string PortalSelf = "portal.self";

    /// <summary>To'liq katalog (28 ta).</summary>
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(DashboardView, "View Dashboard", "DASHBOARD"),
        new(WarehouseView, "View Warehouse", "WAREHOUSE"),
        new(WarehouseManage, "Manage Warehouse", "WAREHOUSE"),
        new(TransfersView, "View Transfers", "TRANSFERS"),
        new(TransfersCreate, "Create Transfers", "TRANSFERS"),
        new(TransfersConfirm, "Confirm Transfers", "TRANSFERS"),
        new(TransfersReject, "Reject Transfers", "TRANSFERS"),
        new(ProductionView, "View Production", "PRODUCTION"),
        new(ProductionManage, "Manage Production", "PRODUCTION"),
        new(FinanceView, "View Finance", "FINANCE"),
        new(FinanceManage, "Manage Finance", "FINANCE"),
        new(KpiView, "View KPI", "KPI"),
        new(KpiManage, "Manage KPI", "KPI"),
        new(PartnersView, "View Partners", "PARTNERS"),
        new(PartnersManage, "Manage Partners", "PARTNERS"),
        new(ProductsView, "View Products", "PRODUCTS"),
        new(ProductsManage, "Manage Products", "PRODUCTS"),
        new(SettingsUsers, "Manage Users", "SETTINGS"),
        new(SettingsRoles, "Manage Roles", "SETTINGS"),
        new(SettingsModules, "Manage Modules", "SETTINGS"),
        new(QualityView, "View Quality", "QUALITY"),
        new(QualityManage, "Manage Quality", "QUALITY"),
        new(AgentsView, "View Agents", "AGENTS"),
        new(AgentsManage, "Manage Agents", "AGENTS"),
        new(AuditView, "View Audit Log", "SETTINGS"),
        new(DeliveryView, "View Delivery", "DELIVERY"),
        new(DeliveryManage, "Manage Delivery", "DELIVERY"),
        new(DocumentsBackdate, "Backdate Documents", "TRANSFERS"),
    ];

    /// <summary>Kod katalogdami.</summary>
    public static bool IsKnown(string code) =>
        All.Any(p => string.Equals(p.Code, code, StringComparison.Ordinal));
}

/// <summary>
/// Identity'dagi YIRIK rol → WMS tizim roli va uning boshlang'ich ruxsatlari (D5).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Identity reyestrida <c>wms</c> mahsulotining <c>product.roles</c> ro'yxati
/// AYNAN <see cref="All"/> bilan bir xil bo'lishi SHART. Ro'yxatda bor, lekin bu
/// yerda yo'q nom Console'da tanlanadigan, lekin WMS'da hech qanday eshik
/// ochmaydigan rol bo'lardi (Wash F4 darsi).
/// </para>
/// <para>
/// Rollar IKKI toifa: <see cref="Ordered"/> — zavod XODIMLARI (ruxsat ladder'i,
/// kattasi yutadi) va <see cref="Portal"/> — tizim foydalanuvchisi bo'lmagan
/// TASHQI odam (mijoz, ta'minotchi, agent). Portal rollarining WMS ruxsati BO'SH:
/// ular ilovaning hech bir ekranini ochmaydi, faqat o'z kabinetini
/// (<c>/api/portal/*</c>) ko'radi. Shunday qilinganining sababi — yangi endpoint
/// qo'shilganda uni portal roliga YOPISHNI unutib bo'lmaydi: u sukut bo'yicha yopiq.
/// </para>
/// <para>
/// To'plamlar — BOSHLANG'ICH qiymat: JIT odamni tizim roliga biriktiradi, keyin
/// tenant admini rolning ruxsatlarini nozik sozlaydi va JIT unga qayta tegmaydi.
/// </para>
/// </remarks>
public static class WmsSystemRoles
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string Employee = "employee";
    public const string Viewer = "viewer";

    /// <summary>Kontragent kabineti (mijoz yoki ta'minotchi) — F9, D8 ning ikkinchi bosqichi.</summary>
    public const string Client = "client";

    /// <summary>Savdo agenti kabineti — o'z mijozlari va komissiyasi.</summary>
    public const string Agent = "agent";

    /// <summary>Xodim rollari, kattadan kichikka — bir odamda bir nechta bo'lsa ENG KATTASI yutadi.</summary>
    public static readonly IReadOnlyList<string> Ordered = [Admin, Manager, Employee, Viewer];

    /// <summary>Kabinet rollari — ilovaga kirmaydi, ruxsat to'plami BO'SH.</summary>
    public static readonly IReadOnlyList<string> Portal = [Agent, Client];

    /// <summary>Tenantda yaratiladigan HAMMA tizim roli.</summary>
    public static readonly IReadOnlyList<string> All = [.. Ordered, .. Portal];

    /// <summary>Kabinet roli (ilova ekranlari yopiq)mi.</summary>
    public static bool IsPortal(string? code) =>
        code is not null && Portal.Contains(code, StringComparer.Ordinal);

    /// <summary>Ko'rsatiladigan nom (tenant o'zgartira oladi).</summary>
    public static string DisplayName(string code) => code switch
    {
        Admin => "Administrator",
        Manager => "Menejer",
        Employee => "Xodim",
        Viewer => "Kuzatuvchi",
        Client => "Mijoz (kabinet)",
        Agent => "Agent (kabinet)",
        _ => code,
    };

    /// <summary>Tizim rolining boshlang'ich ruxsatlari.</summary>
    public static IReadOnlyList<string> PermissionsFor(string code) => code switch
    {
        Admin => [.. WmsPermissions.All.Select(p => p.Code)],

        // Tenant sozlamalaridan tashqari hammasi: foydalanuvchi, rol va modul — tenant
        // egasining qarori.
        Manager =>
        [
            .. WmsPermissions.All
                .Select(p => p.Code)
                .Where(c => c is not (WmsPermissions.SettingsUsers or WmsPermissions.SettingsRoles or WmsPermissions.SettingsModules)),
        ],

        // Sex va ombor xodimi: ko'radi, transfer yaratadi, ishlab chiqarish bosqichini
        // bajaradi va sifat tekshiruvini kiritadi. Tasdiqlash, moliya, sozlamalar — yo'q.
        Employee =>
        [
            WmsPermissions.DashboardView,
            WmsPermissions.WarehouseView,
            WmsPermissions.ProductsView,
            WmsPermissions.PartnersView,
            WmsPermissions.TransfersView,
            WmsPermissions.TransfersCreate,
            WmsPermissions.ProductionView,
            WmsPermissions.ProductionManage,
            WmsPermissions.QualityView,
            WmsPermissions.QualityManage,
            WmsPermissions.KpiView,
            WmsPermissions.DeliveryView,
        ],

        Viewer => [.. WmsPermissions.All.Select(p => p.Code).Where(c => c.EndsWith(".view", StringComparison.Ordinal))],

        // Kabinet rollari (`client`, `agent`) — ATAYLAB bo'sh: ilovaning har bir
        // endpoint'i ruxsat so'raydi, ya'ni ular avtomatik 403 oladi. Kabinet o'z
        // yuzasida (`/api/portal/*`) rol bo'yicha ochiladi.
        _ => [],
    };

    /// <summary>
    /// Tokendagi yirik rollardan tizim rolini tanlaydi; tanilmasa <see langword="null"/>
    /// (fail-closed — hech qanday rol biriktirilmaydi).
    /// </summary>
    /// <remarks>
    /// Xodim roli kabinet rolidan USTUN: bir odam ham zavod xodimi, ham o'z do'koni
    /// bilan mijoz bo'lsa, u ilovani ko'rishda davom etadi.
    /// </remarks>
    public static string? FromTokenRoles(IEnumerable<string>? roles)
    {
        if (roles is null)
        {
            return null;
        }

        HashSet<string> set = new(roles, StringComparer.OrdinalIgnoreCase);
        return Ordered.FirstOrDefault(set.Contains) ?? Portal.FirstOrDefault(set.Contains);
    }
}

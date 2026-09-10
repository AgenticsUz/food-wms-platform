// Platforma paketida ham `ICurrentUser` (Platform.Infrastructure.Identity) va `TenantState`
// (Platform.Infrastructure.Tenancy) bor. API qatlami ikkala namespace'ni ham import qiladi
// (claim nomlari, ICurrentTenant), shuning uchun WMS'niki loyiha bo'ylab BIR MARTA tanlanadi —
// ko'chirilgan har controller'da qayta taxallus yozilmasin.
global using ICurrentUser = WMS.Application.Interfaces.ICurrentUser;
global using TenantState = WMS.Application.Common.TenantState;

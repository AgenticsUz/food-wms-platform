using WMS.Application.Interfaces;

namespace WMS.Tests.Infrastructure;

/// <summary>
/// Testdagi «joriy foydalanuvchi» — ruxsatlari testda oshkora beriladi.
/// </summary>
/// <remarks>
/// <para>
/// Prod'da bu obyektni HTTP qamrovi to'ldiradi (<c>HttpContextCurrentUser</c>), ya'ni DI
/// grafida u FAQAT <c>WMS.API</c> tomonda bor. Servis darajasidagi testlar esa HTTP'siz
/// yuradi — shuning uchun poydevor o'z nusxasini beradi.
/// </para>
/// <para>
/// ⚠️ Sukut bo'yicha ruxsatlar TO'LIQ EMAS, aksincha: har test kerakli ruxsatni O'ZI
/// beradi (<see cref="WmsTenantScope.AsUser"/>). Sabab — «hamma narsaga ruxsat» sukuti
/// ruxsat tekshiruvini tekshiradigan testlarni jimgina yashil qilib qo'yardi.
/// </para>
/// </remarks>
public sealed class TestCurrentUser : ICurrentUser
{
    private HashSet<string> _permissions = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Guid? Sub { get; private set; }

    /// <inheritdoc />
    public Guid? ProfileId { get; private set; }

    /// <inheritdoc />
    public string? FullName { get; private set; } = "Test Xodim";

    /// <inheritdoc />
    public bool IsAuthenticated => Sub is not null;

    /// <inheritdoc />
    public bool IsPlatformAdmin { get; private set; }

    /// <inheritdoc />
    public bool IsConsoleOperator { get; private set; }

    /// <inheritdoc />
    public IReadOnlySet<string> Permissions => _permissions;

    /// <inheritdoc />
    public bool HasPermission(string permission) => _permissions.Contains(permission);

    /// <summary>Foydalanuvchini va uning ruxsatlarini o'rnatadi.</summary>
    /// <param name="profileId"><c>user_profile.id</c>.</param>
    /// <param name="permissions">Ruxsat kodlari.</param>
    /// <param name="fullName">Ism.</param>
    public void Set(Guid? profileId, IEnumerable<string> permissions, string fullName = "Test Xodim")
    {
        ArgumentNullException.ThrowIfNull(permissions);

        Sub = profileId is null ? null : Guid.CreateVersion7();
        ProfileId = profileId;
        FullName = fullName;
        _permissions = new HashSet<string>(permissions, StringComparer.Ordinal);
    }
}

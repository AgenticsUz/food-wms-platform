using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

/// <summary>
/// <c>wms.telegram_link</c> — Telegram chatining tenant ichidagi egaga bog'lanishi (Wash naqshi).
/// </summary>
/// <remarks>
/// <para>
/// Ilgari <c>user_profile.telegram_chat_id</c> ustuni edi. Alohida jadval — chunki TG12/TG13 da
/// haydovchi va kontragent ham ulanadi va «bu chat kimniki» savoliga uch jadvaldan emas,
/// bittadan javob bo'lsin. <see cref="UserProfileId"/> (keyin <c>DriverId</c>, <c>CounterpartyId</c>)
/// dan aynan bittasi to'la.
/// </para>
/// <para>
/// Bloklaganda yozuv O'CHIRILMAYDI — <see cref="IsActive"/> = <see langword="false"/>: qaytib
/// kelsa tarix va sozlamalari (<see cref="MutedTypes"/>) joyida. Bir egaga bitta yozuv: boshqa
/// akkauntdan qayta ulansa <see cref="ChatId"/> ALMASHADI, ikkinchi qator yaratilmaydi — aks
/// holda xabar ikki chatga ketardi.
/// </para>
/// </remarks>
public class TelegramLink : TenantEntity
{
    public Guid? UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }

    /// <summary>Telegram chat identifikatori (int64; shaxsiy chatda foydalanuvchi id'si bilan bir xil).</summary>
    public long ChatId { get; set; }

    public string? Username { get; set; }
    public string? FirstName { get; set; }

    /// <summary>Bot shu tilda gapiradi — Telegram <c>language_code</c> dan (<c>uz</c>/<c>ru</c>).</summary>
    public string Lang { get; set; } = "uz";

    public TelegramLinkSource LinkedVia { get; set; } = TelegramLinkSource.DeepLink;

    public bool IsActive { get; set; } = true;

    /// <summary>Oxirgi ulanish (qayta ulanishda yangilanadi; <c>CreatedAt</c> — birinchisi).</summary>
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Foydalanuvchi o'chirgan bildirishnoma turlari — <c>NotificationType</c> nomlari, vergul bilan
    /// (TG3). Bitmask emas: enum kengayganda o'qish oson. <see langword="null"/> — hech narsa o'chirilmagan.
    /// </summary>
    public string? MutedTypes { get; set; }

    /// <summary>Kunlik xulosa (TG11). Sukut yoqiq — qabul qiluvchi baribir `dashboard.view` bilan cheklanadi.</summary>
    public bool Digest { get; set; } = true;
}

/// <summary>
/// <c>wms.telegram_link_token</c> — bir martalik deep-link tokeni. PLATFORMA jadvali (RLS yo'q).
/// </summary>
/// <remarks>
/// <c>/start &lt;token&gt;</c> kelganda tenant konteksti YO'Q — token RLS ostida bo'lsa uni topib
/// bo'lmasdi (0 qator). Topilgach tenant scope'i ochiladi va <see cref="TelegramLink"/> yoziladi.
/// Tasodifiy 24 bayt (base64url, 32 belgi) — Telegram payload chegarasi 64 belgi <c>[A-Za-z0-9_-]</c>.
/// </remarks>
public class TelegramLinkToken : BaseEntity
{
    public string Token { get; set; } = null!;
    public Guid TenantId { get; set; }
    public TelegramLinkSubject SubjectType { get; set; } = TelegramLinkSubject.UserProfile;

    /// <summary><c>user_profile.id</c> (keyin haydovchi/kontragent id'si).</summary>
    public Guid SubjectId { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

/// <summary>
/// <c>wms.telegram_group</c> — tenantning Telegram guruhi (TG16): umumiy bildirishnomalar shaxsiy chatlarga
/// QO'SHIMCHA guruhga ham ketadi. PLATFORMA jadvali (RLS yo'q): guruh yangilanishi tenant kontekstisiz keladi.
/// </summary>
/// <remarks>
/// Ulash — admin (<c>settings.modules</c>, shaxsiy ulanishi bor) guruhda <c>/ulash</c> yozadi. Bir guruh — bir
/// tenant. Guruhdagi tugmani bosgan odamning aktori — uning shu tenantdagi SHAXSIY ulanishi; ulanmagan bo'lsa
/// «avval profilingizni ulang». Bot guruhdan chiqarilsa <see cref="IsActive"/> = <see langword="false"/>.
/// </remarks>
public class TelegramGroup : BaseEntity
{
    public long ChatId { get; set; }
    public Guid TenantId { get; set; }
    public string? Title { get; set; }

    /// <summary>Guruhga bormaydigan turlar (<c>NotificationType</c> nomlari, vergul bilan) — <c>/sozlash</c>.</summary>
    public string? MutedTypes { get; set; }

    public Guid? LinkedByProfileId { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// <c>wms.telegram_chat_state</c> — ko'p tenantli chatning bir soatlik tenant tanlovi (TG10). PLATFORMA
/// jadvali (RLS yo'q): so'rov buyrug'i kelganda tenant hali noma'lum.
/// </summary>
public class TelegramChatState : BaseEntity
{
    public long ChatId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// <c>wms.telegram_outbox</c> — yuboriladigan Telegram xabari. PLATFORMA jadvali (RLS yo'q):
/// yuboruvchi fon xizmati bitta scope'da hamma tenantning navbatini o'qiydi.
/// </summary>
/// <remarks>
/// <para>
/// HTTP chaqiruv so'rov ichida BO'LMAYDI: ilgari <c>NotificationService</c> har ulangan
/// foydalanuvchi uchun ketma-ket (10 s gacha) kutardi va transfer tasdig'i shuncha osilardi.
/// Qator bildirishnoma bilan BITTA <c>SaveChanges</c> da yoziladi, yuborish — <c>TelegramOutboxBackgroundService</c>.
/// </para>
/// <para>
/// <see cref="DedupKey"/> unique: ayni hodisa ikki marta ishlansa (qayta urinish, ikki yugurish)
/// ikkinchi qator YOZILMAYDI (Wash darsi). <see cref="TenantId"/> va <see cref="TelegramLinkId"/>
/// — 403 da ulanishni uzish uchun (scope shu bilan ochiladi); platforma egasi kanalida (TG17)
/// ikkalasi bo'sh.
/// </para>
/// </remarks>
public class TelegramOutbox : BaseEntity
{
    /// <summary>Nechta urinishdan keyin taslim bo'ladi.</summary>
    public const int MaxAttempts = 5;

    /// <summary>Telegram matn chegarasi.</summary>
    public const int MaxTextLength = 4096;

    public Guid? TenantId { get; set; }
    public Guid? TelegramLinkId { get; set; }
    public long ChatId { get; set; }

    /// <summary>Tayyor HTML — yuborilgandan keyin ham o'zgarmaydi.</summary>
    public string Text { get; set; } = null!;

    /// <summary>Izlash uchun; FK EMAS — bildirishnoma tenant jadvalida.</summary>
    public Guid? NotificationId { get; set; }

    public string? DedupKey { get; set; }
    public TelegramOutboxStatus Status { get; set; } = TelegramOutboxStatus.Pending;
    public int Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>Faqat HTTP kod va Telegram <c>description</c> — so'rov manzili (token) EMAS.</summary>
    public string? LastError { get; set; }

    public DateTime? SentAt { get; set; }

    /// <summary>Qator turi: xabar yoki mavjud xabarning tugmalarini olib tashlash (TG9).</summary>
    public TelegramOutboxKind Kind { get; set; } = TelegramOutboxKind.Message;

    /// <summary>Inline tugmalar — tayyor <c>reply_markup</c> JSON (TG9); yo'q — oddiy xabar.</summary>
    public string? ReplyMarkup { get; set; }

    /// <summary>
    /// Yuborilgan xabarning Telegram id'si (<see cref="TelegramOutboxKind.Message"/>) yoki tahrirlanadigan
    /// xabar (<see cref="TelegramOutboxKind.RemoveButtons"/>). Tugma bosilganda navbat qatori
    /// <c>(chat_id, message_id)</c> bo'yicha topiladi — tenant va aktor shundan.
    /// </summary>
    public long? MessageId { get; set; }

    /// <summary>Orqaga chekinish: 1, 2, 4, 8 daqiqa (Wash <c>BackoffFor</c>).</summary>
    public static TimeSpan BackoffFor(int attempt) =>
        TimeSpan.FromMinutes(Math.Pow(2, Math.Clamp(attempt - 1, 0, 3)));
}

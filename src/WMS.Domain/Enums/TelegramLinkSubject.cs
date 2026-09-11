namespace WMS.Domain.Enums;

/// <summary>Deep-link tokeni kimni ulaydi. Haydovchi va kontragent — TG12/TG13.</summary>
public enum TelegramLinkSubject
{
    UserProfile = 1,
    Driver = 2,
    Counterparty = 3,
}

/// <summary>Chat qanday ulangani.</summary>
public enum TelegramLinkSource
{
    /// <summary><c>t.me/bot?start=&lt;token&gt;</c>.</summary>
    DeepLink = 1,

    /// <summary>Botda «Kontaktni ulashish» (TG12 muqobili, hozircha ishlatilmaydi).</summary>
    ContactShare = 2,
}

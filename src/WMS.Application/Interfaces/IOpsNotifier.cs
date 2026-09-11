namespace WMS.Application.Interfaces;

/// <summary>
/// Platforma egasining Telegram kanali (TG17): tenant hodisalari va nosozliklar — mijoz ma'lumotisiz.
/// <c>Telegram:OpsChatId</c> bo'sh bo'lsa no-op. Navbat orqali (so'rov ichida HTTP yo'q).
/// </summary>
public interface IOpsNotifier
{
    bool IsEnabled { get; }

    /// <param name="text">HTML; chaqiruvchi o'zi kodlaydi (tenant kodi/nomi kabi qiymatlarni).</param>
    /// <param name="dedupKey">Bir xil hodisa ikki marta ketmasin (masalan <c>ops:tenant-new:{id}</c>); <see langword="null"/> — har safar.</param>
    Task SendAsync(string text, string? dedupKey, CancellationToken cancellationToken);
}

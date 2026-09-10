namespace WMS.Application.Interfaces;

/// <summary>
/// The language of the request being handled, resolved once from Accept-Language.
/// Services need it when they build user-facing text outside an exception — for example the
/// "why are you blocked" message on /api/subscription/me.
/// </summary>
public interface IRequestLanguage
{
    /// "uz" | "ru" | "en" — always one of the supported languages, never null.
    string Current { get; }
}

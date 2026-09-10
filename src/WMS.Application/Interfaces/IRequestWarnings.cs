namespace WMS.Application.Interfaces;

/// <summary>
/// A place for a service to leave a non-fatal notice that the response filter attaches to
/// <c>ApiResponse.Warning</c> — "you created the user, and you are now at 20 of 25".
///
/// It is not an exception and not an error: the operation succeeded, the status stays 200,
/// and a client that ignores the field behaves exactly as before. Scoped to the request.
/// </summary>
public interface IRequestWarnings
{
    /// <param name="code">Machine-readable, never translated (e.g. "limit_warn_users").</param>
    /// <param name="messageTemplate">English template — the translation key.</param>
    void Add(string code, string messageTemplate, params object?[] args);

    /// The first warning raised, or null. One is enough: a wall of notices is ignored.
    (string Code, string Template, object?[] Args)? First { get; }
}

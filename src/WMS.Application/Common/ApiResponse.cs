namespace WMS.Application.Common;

/// A non-fatal notice attached to a SUCCESSFUL response. Not an error: the status stays
/// 200/201 and Success stays true — a client that ignores it behaves exactly as before.
public class ApiWarning
{
    /// Machine-readable, never translated: "limit_warn_users" | "limit_warn_warehouses" |
    /// "limit_warn_transfers".
    public string Code { get; set; } = null!;
    public string? Message { get; set; }
}

/// Lets a result filter reach Message and Warning on any ApiResponse&lt;T&gt; without reflection.
public interface IApiResponse
{
    string? Message { get; set; }
    ApiWarning? Warning { get; set; }
}

public class ApiResponse<T> : IApiResponse
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    /// Machine-readable error code for cases the client must react to specifically
    /// (subscription / entitlement refusals). Null for ordinary validation errors.
    public string? Code { get; set; }

    /// Set by the response filter when a service raised one (e.g. a plan limit at 80%).
    public ApiWarning? Warning { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };

    public static ApiResponse<T> Fail(string message, string code) =>
        new() { Success = false, Message = message, Code = code };
}

namespace WMS.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    /// Machine-readable error code for cases the client must react to specifically
    /// (subscription / entitlement refusals). Null for ordinary validation errors.
    public string? Code { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };

    public static ApiResponse<T> Fail(string message, string code) =>
        new() { Success = false, Message = message, Code = code };
}

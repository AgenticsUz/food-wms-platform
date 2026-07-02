namespace WMS.Application.Common;

/// Business-rule violation — mapped to HTTP 400 by the exception middleware.
public class AppException : Exception
{
    public AppException(string message) : base(message) { }
}

/// Requested entity does not exist (in the caller's tenant) — mapped to HTTP 404.
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

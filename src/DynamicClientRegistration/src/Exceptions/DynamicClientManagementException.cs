using System.Net;

namespace Open.IdentityServer.DynamicClientRegistration.Exceptions;

public abstract class DynamicClientManagementException : Exception
{
    protected DynamicClientManagementException(
        string error,
        string? errorDescription = null,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        Exception? innerException = null)
        : base(errorDescription ?? error, innerException)
    {
        Error = error;
        ErrorDescription = errorDescription;
        StatusCode = statusCode;
    }

    public string Error { get; }
    public string? ErrorDescription { get; }
    public HttpStatusCode StatusCode { get; }
}
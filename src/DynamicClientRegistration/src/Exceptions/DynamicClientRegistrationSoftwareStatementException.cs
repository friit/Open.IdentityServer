using System.Net;

namespace Open.IdentityServer.DynamicClientRegistration.Exceptions;

public class DynamicClientRegistrationSoftwareStatementException : DynamicClientManagementException
{
    public DynamicClientRegistrationSoftwareStatementException(string error, string? errorDescription = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest, Exception? innerException = null) : base(error, errorDescription, statusCode, innerException)
    {
    }
}
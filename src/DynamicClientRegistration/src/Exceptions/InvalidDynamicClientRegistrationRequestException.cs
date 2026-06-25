using System.Net;

namespace Open.IdentityServer.DynamicClientRegistration.Exceptions;

public class InvalidDynamicClientRegistrationRequestException : DynamicClientManagementException
{
    public InvalidDynamicClientRegistrationRequestException(string error, string? errorDescription = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest, Exception? innerException = null) : base(error, errorDescription, statusCode, innerException)
    {
    }
}
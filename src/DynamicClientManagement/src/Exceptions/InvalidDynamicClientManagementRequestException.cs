using System.Net;

namespace Open.IdentityServer.DynamicClientManagement.Exceptions;

public class InvalidDynamicClientManagementRequestException : DynamicClientManagementException
{
    public InvalidDynamicClientManagementRequestException(string error, string? errorDescription = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest, Exception? innerException = null) : base(error, errorDescription, statusCode, innerException)
    {
    }
}
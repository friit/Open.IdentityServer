using System.Net;

namespace Open.IdentityServer.DynamicClientRegistration.Exceptions;

public sealed class DynamicClientRegistrationParseException
    : DynamicClientManagementException
{
    public DynamicClientRegistrationParseException(
        string error,
        string? errorDescription = null,
        Exception? innerException = null)
        : base(error, errorDescription, HttpStatusCode.BadRequest, innerException)
    {
    }
}
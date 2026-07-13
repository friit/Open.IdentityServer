using System.Net;

namespace Open.IdentityServer.DynamicClientManagement.Exceptions;

public sealed class DynamicClientManagementParseException
    : DynamicClientManagementException
{
    public DynamicClientManagementParseException(
        string error,
        string? errorDescription = null,
        Exception? innerException = null)
        : base(error, errorDescription, HttpStatusCode.BadRequest, innerException)
    {
    }
}
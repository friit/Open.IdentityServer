using Open.IdentityServer.DynamicClientRegistration.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Parser;

public interface IDynamicClientRegistrationRequestParser
{
    Task<RegisterRequest> ParseAsync(Stream body, string? contentType, CancellationToken cancellationToken);
}
using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Models.Results;
using Open.IdentityServer.DynamicClientRegistration.Parser.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Parser;

public interface IDynamicClientRegistrationRequestParser
{
    Task<RegistrationRequestParseResult> ParseAsync(Stream body, string? contentType, CancellationToken cancellationToken);
}
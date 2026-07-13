using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;

namespace Open.IdentityServer.DynamicClientManagement.Parser;

public interface IDynamicClientManagementRequestParser
{
    Task<RegistrationRequestParseResult> ParseAsync(Stream body, string? contentType, CancellationToken cancellationToken);
}
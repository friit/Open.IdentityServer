using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation;

public interface IDynamicClientManagementValidator
{
    Task<RegistrationRequestValidationResult> ValidateAsync(
        RegistrationRequestParseResult request, 
        CancellationToken cancellationToken = default
    );
}
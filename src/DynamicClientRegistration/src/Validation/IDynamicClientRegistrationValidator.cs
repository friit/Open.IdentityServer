using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Models.Results;
using Open.IdentityServer.DynamicClientRegistration.Parser.Models;
using Open.IdentityServer.DynamicClientRegistration.Validation.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Validation;

public interface IDynamicClientRegistrationValidator
{
    Task<RegistrationRequestValidationResult> ValidateAsync(
        RegistrationRequestParseResult request, 
        CancellationToken cancellationToken = default
    );
}
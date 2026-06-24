using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Models.Results;

namespace Open.IdentityServer.DynamicClientRegistration.Validation;

public interface IDynamicClientRegistrationValidator
{
    Task<DynamicClientRegistrationValidationResult> ValidateAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
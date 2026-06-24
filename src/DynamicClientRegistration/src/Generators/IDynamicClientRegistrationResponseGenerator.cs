using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Models.Results;

namespace Open.IdentityServer.DynamicClientRegistration.Generators;

public interface IDynamicClientRegistrationResponseGenerator
{
    Task<RegisterResponse> GenerateAsync(DynamicClientRegistrationResult registrationResult, CancellationToken cancellationToken = default);
}
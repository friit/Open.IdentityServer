using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Models.Results;

namespace Open.IdentityServer.DynamicClientRegistration.Services;

public interface IDynamicClientRegistrationService
{
    Task<DynamicClientRegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
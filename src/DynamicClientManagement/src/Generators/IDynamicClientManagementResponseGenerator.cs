using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;

namespace Open.IdentityServer.DynamicClientManagement.Generators;

public interface IDynamicClientManagementResponseGenerator
{
    Task<RegisterResponse> GenerateAsync(DynamicClientManagementResult registrationResult, CancellationToken cancellationToken = default);
}
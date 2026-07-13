using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;

namespace Open.IdentityServer.DynamicClientManagement.Services;

public interface IDynamicClientManagementService
{
    Task<DynamicClientManagementResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Stores;

public interface IDynamicClientManagementStore
{
    Task RegisterClientAsync(Client client, CancellationToken cancellationToken = default);
}

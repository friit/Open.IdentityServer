using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Stores;

public interface IDynamicClientManagementStore
{
    Task RegisterClientAsync(Client client, CancellationToken cancellationToken = default);
}
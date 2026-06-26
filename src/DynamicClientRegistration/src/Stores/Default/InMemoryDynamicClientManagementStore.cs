using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Stores.Default;

public class InMemoryDynamicClientManagementStore : IDynamicClientManagementStore
{
    private readonly ICollection<Client> _clients;

    public InMemoryDynamicClientManagementStore(ICollection<Client> clients)
    {
        _clients = clients;
    }
    public Task RegisterClientAsync(Client client, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if(_clients.Contains(client))
        {
            throw new InvalidOperationException($"Client with id {client.ClientId} is already registered.");
        }
        
        _clients.Add(client);
        return Task.CompletedTask;
    }
}
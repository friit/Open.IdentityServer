using Microsoft.Extensions.Logging;
using Open.IdentityServer.DynamicClientManagement.Stores;
using Open.IdentityServer.EntityFramework.Interfaces;
using Open.IdentityServer.EntityFramework.Mappers;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.EntityFramework.Stores;

public class DynamicClientManagementStore : IDynamicClientManagementStore
{
    protected readonly IConfigurationDbContext Context;
    protected readonly ILogger<DynamicClientManagementStore> Logger;

    public DynamicClientManagementStore(IConfigurationDbContext context, ILogger<DynamicClientManagementStore> logger)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Logger = logger;
    }
    public async Task RegisterClientAsync(Client client, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(client);

        var existing = Context.Clients.FirstOrDefault(c => c.ClientId == client.ClientId);
        if (existing != null)
        {
            throw new InvalidOperationException($"Client with id {client.ClientId} is already registered.");
        }
        
        Context.Clients.Add(client.ToEntity());
        
        await Context.SaveChangesAsync();
    }
}
using Microsoft.EntityFrameworkCore;
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

    public async Task<Client> FindClientByIdAsync(string clientId)
    {
        var baseQuery = Context.Clients
            .Where(client => client.ClientId == clientId);

        var client = (await baseQuery.ToArrayAsync())
            .SingleOrDefault(candidate => candidate.ClientId == clientId);
        if (client == null)
        {
            return null!;
        }

        await baseQuery.Include(x => x.AllowedCorsOrigins).SelectMany(c => c.AllowedCorsOrigins).LoadAsync();
        await baseQuery.Include(x => x.AllowedGrantTypes).SelectMany(c => c.AllowedGrantTypes).LoadAsync();
        await baseQuery.Include(x => x.AllowedScopes).SelectMany(c => c.AllowedScopes).LoadAsync();
        await baseQuery.Include(x => x.Claims).SelectMany(c => c.Claims).LoadAsync();
        await baseQuery.Include(x => x.ClientSecrets).SelectMany(c => c.ClientSecrets).LoadAsync();
        await baseQuery.Include(x => x.IdentityProviderRestrictions).SelectMany(c => c.IdentityProviderRestrictions).LoadAsync();
        await baseQuery.Include(x => x.PostLogoutRedirectUris).SelectMany(c => c.PostLogoutRedirectUris).LoadAsync();
        await baseQuery.Include(x => x.Properties).SelectMany(c => c.Properties).LoadAsync();
        await baseQuery.Include(x => x.RedirectUris).SelectMany(c => c.RedirectUris).LoadAsync();

        return client.ToModel();
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
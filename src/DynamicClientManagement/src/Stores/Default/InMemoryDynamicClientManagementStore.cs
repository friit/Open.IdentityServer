// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Stores.Default;

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
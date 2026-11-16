// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.Models;
using Open.IdentityServer.Stores;

namespace Open.IdentityServer.DynamicClientManagement.Stores.Default;

public sealed class InMemoryDynamicClientStore : IClientStore
{
    private readonly IEnumerable<Client> _clients;

    public InMemoryDynamicClientStore(IEnumerable<Client> clients)
    {
        _clients = clients;
    }

    public Task<Client> FindClientByIdAsync(string clientId)
    {
        var client = _clients.SingleOrDefault(candidate => candidate.ClientId == clientId);

        return Task.FromResult(client!);
    }
}

// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.Models;
using Open.IdentityServer.Stores;

namespace Open.IdentityServer.DynamicClientManagement.Stores;

public interface IDynamicClientManagementStore : IClientStore
{
    Task RegisterClientAsync(Client client, CancellationToken cancellationToken = default);
}

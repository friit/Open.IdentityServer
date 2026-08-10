// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Stores;

public interface IDynamicClientManagementStore
{
    Task RegisterClientAsync(Client client, CancellationToken cancellationToken = default);
}

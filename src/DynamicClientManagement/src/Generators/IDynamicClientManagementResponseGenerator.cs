// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Services.Results;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Generators;

public interface IDynamicClientManagementResponseGenerator
{
    Task<ClientResponseDTO> GenerateGetResponseAsync(Client client, CancellationToken cancellationToken = default);
    Task<ClientResponseDTO> GenerateRegisterResponseAsync(DynamicClientManagementClientResult registrationClientResult, CancellationToken cancellationToken = default);
}
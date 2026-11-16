// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Services.Results;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Services;

public interface IDynamicClientManagementService
{
    Task<DynamicClientManagementClientResult> RegisterAsync(RegisterRequestDTO requestDto, CancellationToken cancellationToken = default);
}
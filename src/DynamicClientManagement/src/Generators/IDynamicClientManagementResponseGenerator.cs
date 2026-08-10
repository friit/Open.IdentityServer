// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;

namespace Open.IdentityServer.DynamicClientManagement.Generators;

public interface IDynamicClientManagementResponseGenerator
{
    Task<RegisterResponse> GenerateAsync(DynamicClientManagementResult registrationResult, CancellationToken cancellationToken = default);
}
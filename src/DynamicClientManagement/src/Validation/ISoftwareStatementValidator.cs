// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation;

public interface ISoftwareStatementValidator
{
    Task<SoftwareStatementValidationResult> ValidateAsync(
        JwtSecurityToken softwareStatement,
        CancellationToken cancellationToken);
}
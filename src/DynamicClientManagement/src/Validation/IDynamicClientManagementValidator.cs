// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation;

public interface IDynamicClientManagementValidator
{
    Task<RegistrationRequestValidationResult> ValidateAsync(
        RegistrationRequestParseResult request, 
        CancellationToken cancellationToken = default
    );
}
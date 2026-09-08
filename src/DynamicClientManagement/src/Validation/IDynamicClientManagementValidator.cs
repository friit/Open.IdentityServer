// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation;

/// <summary>
/// Validates dynamic client registration requests.
/// </summary>
public interface IDynamicClientManagementValidator
{
    /// <summary>
    /// Validates a parsed registration request and, when present, the supplied software statement.
    /// </summary>
    /// <param name="request">The parsed registration request to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The validation result.</returns>
    Task<RegistrationRequestValidationResult> ValidateAsync(
        RegistrationRequestParseResult request, 
        CancellationToken cancellationToken = default
    );

    Task<bool> ValidateTokenAsync(Client client, string token);
}
// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Services.Results;

/// <summary>
/// Returned by DynamicClientManagementService.RegisterAsync
/// </summary>
public sealed record DynamicClientManagementClientResult
{
    public required Client Client { get; init; }
    public required DateTimeOffset ClientIdIssuedAt { get; init; }
    public string? ClientSecret { get; init; }
    public DateTimeOffset? ClientSecretExpiresAt { get; init; }
    public string? RegistrationAccessToken { get; init; }
    public string? RegistrationClientUri { get; init; }
    public RegisterRequestDTO OriginalRequestDto { get; init; }
}


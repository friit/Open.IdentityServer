// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Models.Results;

public sealed class DynamicClientManagementResult
{
    public required Client Client { get; init; }
    public string? ClientSecret { get; init; }
    public required string RegistrationAccessToken { get; init; }
    public required string RegistrationClientUri { get; init; }
    public required DateTimeOffset ClientIdIssuedAt { get; init; }
    public DateTimeOffset? ClientSecretExpiresAt { get; init; }
    public required RegisterRequest EffectiveRequest { get; init; }
}
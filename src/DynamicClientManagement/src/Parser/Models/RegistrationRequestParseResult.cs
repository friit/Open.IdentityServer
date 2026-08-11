// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using Open.IdentityServer.DynamicClientManagement.Models;

namespace Open.IdentityServer.DynamicClientManagement.Parser.Models;

/// <summary>
/// Represents the result of parsing a registration request.
/// </summary>
public sealed class RegistrationRequestParseResult
{
    /// <summary>
    /// Gets the parsed registration request.
    /// The Request is parsed from JSON and merged with the values from the software statement, if present.
    /// </summary>
    public required RegisterRequest Request { get; init; }

    /// <summary>
    /// Gets the optional software statement extracted from the request.
    /// </summary>
    public JwtSecurityToken? SoftwareStatement { get; init; }
}
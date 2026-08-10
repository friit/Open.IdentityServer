// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using Open.IdentityServer.DynamicClientManagement.Models;

namespace Open.IdentityServer.DynamicClientManagement.Parser.Models;

public sealed class RegistrationRequestParseResult
{
    public required RegisterRequest Request { get; init; }

    public JwtSecurityToken? SoftwareStatement { get; init; }
}
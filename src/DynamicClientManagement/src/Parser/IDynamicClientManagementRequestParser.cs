// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Parser.Models;

namespace Open.IdentityServer.DynamicClientManagement.Parser;

public interface IDynamicClientManagementRequestParser
{
    Task<RegistrationRequestParseResult> ParseAsync(Stream body, string? contentType, CancellationToken cancellationToken);
}
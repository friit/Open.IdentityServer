// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;

namespace Open.IdentityServer.DynamicClientManagement.Parser;

public interface IDynamicClientManagementRequestParser
{
    Task<GetRequestParseResult> ParseGetRequestAsync(HttpContext context, string client_id, CancellationToken cancellationToken);
    Task<RegistrationRequestParseResult> ParseRegisterRequestAsync(Stream body, string? contentType, CancellationToken cancellationToken);
}
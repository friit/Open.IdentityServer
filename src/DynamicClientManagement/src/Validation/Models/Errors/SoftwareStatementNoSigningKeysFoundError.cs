// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;

public sealed record SoftwareStatementNoSigningKeysFoundError(string Issuer)
    : SoftwareStatementValidationError($"No signing keys found for issuer: {Issuer}");

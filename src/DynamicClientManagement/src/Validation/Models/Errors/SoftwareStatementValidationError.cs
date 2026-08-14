// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;

public abstract record SoftwareStatementValidationError(string ErrorDescription)
    : ValidationError(Constants.RegisterErrors.InvalidSoftwareStatement, ErrorDescription);

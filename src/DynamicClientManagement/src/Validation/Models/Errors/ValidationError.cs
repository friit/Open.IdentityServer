// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;

public abstract record ValidationError(string Error, string? ErrorDescription = null)
{
    public string ToMessage() => ErrorDescription is null ? Error : $"{Error}: {ErrorDescription}";
}

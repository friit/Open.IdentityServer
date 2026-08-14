// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models;

public class RegistrationRequestValidationResult
{
    public bool IsError => Errors.Length != 0;
    public ValidationError[] Errors { get; private set; }

    public RegisterRequest? Request { get; private set; }

    public RegistrationRequestValidationResult(RegisterRequest request)
    {
        Request = request;
        Errors = Array.Empty<ValidationError>();
    }

    public RegistrationRequestValidationResult(
        RegisterRequest request,
        ValidationError error)
        : this(request)
    {
        Errors = [error];
    }
    
    public void AddError(ValidationError error)
    {
        Errors = Errors.Append(error).ToArray();
    }

}
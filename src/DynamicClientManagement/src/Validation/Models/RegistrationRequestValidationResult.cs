// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models;

public class RegistrationRequestValidationResult
{
    public bool IsError => Errors.Length != 0;
    public ValidationError[] Errors { get; private set; }

    public RegisterRequestDTO? RequestDto { get; private set; }

    public RegistrationRequestValidationResult(RegisterRequestDTO requestDto)
    {
        RequestDto = requestDto;
        Errors = Array.Empty<ValidationError>();
    }

    public RegistrationRequestValidationResult(
        RegisterRequestDTO requestDto,
        ValidationError error)
        : this(requestDto)
    {
        Errors = [error];
    }
    
    public void AddError(ValidationError error)
    {
        Errors = Errors.Append(error).ToArray();
    }

}
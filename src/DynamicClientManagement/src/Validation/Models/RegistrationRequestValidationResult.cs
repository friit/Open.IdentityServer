// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models;

public class RegistrationRequestValidationResult
{
    public bool IsError => Errors.Length != 0;



    public string[] Errors { get; set; }

    public RegisterRequest? Request { get; private set; }

    public RegistrationRequestValidationResult(RegisterRequest request)
    {
        Request = request;
        Errors = Array.Empty<string>();
    }

    public RegistrationRequestValidationResult(
        RegisterRequest request,
        string error,
        string? errorDescription = null)
        : this(request)
    {

        Errors = [errorDescription is null ? error : $"{error}: {errorDescription}"];
    }
    
    public void AddError(string error, string? errorDescription = null)
    {
        Errors = Errors.Append(
            errorDescription is null ? error : $"{error}: {errorDescription}"
        ).ToArray();
    }

}
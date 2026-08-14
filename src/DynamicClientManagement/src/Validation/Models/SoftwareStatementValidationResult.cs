// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models;

public class SoftwareStatementValidationResult
{
    public bool IsError => Error is not null;
    public ValidationError? Error { get; private set; }
    
    public JwtSecurityToken? SoftwareStatement { get; private set; }

    public SoftwareStatementValidationResult(JwtSecurityToken softwareStatement)
    {
        SoftwareStatement = softwareStatement;
    }
    
    public SoftwareStatementValidationResult(JwtSecurityToken softwareStatement, ValidationError error)
    {
        SoftwareStatement = softwareStatement;
        Error = error;
    }
}
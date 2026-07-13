using System.IdentityModel.Tokens.Jwt;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models;

public class SoftwareStatementValidationResult
{
    public bool IsError { get; set; }
    public string? ErrorDescription { get; set; }
    public string Error { get; set; }
    
    public JwtSecurityToken? SoftwareStatement { get; private set; }

    public SoftwareStatementValidationResult(JwtSecurityToken softwareStatement)
    {
        SoftwareStatement = softwareStatement;
        IsError = false;
    }
    
    public SoftwareStatementValidationResult(JwtSecurityToken softwareStatement, string error, string? errorDescription = null) : base()
    {
        SoftwareStatement = softwareStatement;
        IsError = true;
        Error = error;
        ErrorDescription = errorDescription;
    }
}
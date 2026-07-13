using System.IdentityModel.Tokens.Jwt;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation;

public interface ISoftwareStatementValidator
{
    Task<SoftwareStatementValidationResult> ValidateAsync(
        JwtSecurityToken softwareStatement,
        CancellationToken cancellationToken);
}
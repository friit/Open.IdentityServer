using System.IdentityModel.Tokens.Jwt;
using Open.IdentityServer.DynamicClientRegistration.Validation.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Validation;

public interface ISoftwareStatementValidator
{
    Task<SoftwareStatementValidationResult> ValidateAsync(
        JwtSecurityToken softwareStatement,
        CancellationToken cancellationToken);
}
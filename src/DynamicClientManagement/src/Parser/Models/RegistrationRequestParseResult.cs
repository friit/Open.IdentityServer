using System.IdentityModel.Tokens.Jwt;
using Open.IdentityServer.DynamicClientManagement.Models;

namespace Open.IdentityServer.DynamicClientManagement.Parser.Models;

public sealed class RegistrationRequestParseResult
{
    public required RegisterRequest Request { get; init; }

    public JwtSecurityToken? SoftwareStatement { get; init; }
}
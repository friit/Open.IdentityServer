using System.IdentityModel.Tokens.Jwt;
using Open.IdentityServer.DynamicClientRegistration.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Parser.Models;

public sealed class RegistrationRequestParseResult
{
    public required RegisterRequest Request { get; init; }

    public JwtSecurityToken? SoftwareStatement { get; init; }
}
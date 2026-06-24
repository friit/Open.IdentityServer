using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Models.Results;

public sealed class DynamicClientRegistrationResult
{
    public required Client Client { get; init; }
    public required string ClientSecret { get; init; }
    public required string RegistrationAccessToken { get; init; }
    public required string RegistrationClientUri { get; init; }
    public required DateTimeOffset ClientIdIssuedAt { get; init; }
    public DateTimeOffset? ClientSecretExpiresAt { get; init; }
    public required RegisterRequest EffectiveRequest { get; init; }
}
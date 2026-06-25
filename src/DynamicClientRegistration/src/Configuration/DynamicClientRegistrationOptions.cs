using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Open.IdentityServer.DynamicClientRegistration.Configuration;

public class DynamicClientRegistrationOptions
{
    /// <summary>
    /// If true, the software statement must contain a valid JWT.
    /// </summary>
    public bool RequireValidSoftwareStatement { get; set; } = false;
    
    /// <summary>
    /// If true, the software statement must contain a valid signature.
    /// </summary>
    public bool RequireValidSoftwareStatementSignature { get; set; } = true;

    /// <summary>
    /// Configures the built-in software statement signature validator.
    /// Custom ISoftwareStatementSignatureValidator implementations may ignore these settings.
    /// </summary>
    public SoftwareStatementSignatureValidationOptions SoftwareStatementSignatureValidationOptions { get; } = new();

    public JsonSerializerOptions? ParserJsonSerializerOptions { get; set; }
}

public class SoftwareStatementSignatureValidationOptions
{
    public Dictionary<string, ICollection<SecurityKey>> IssuerSigningKeys { get; } = [];

    public Dictionary<string, Uri> IssuerJwksUris { get; } = [];

    public TimeSpan JwksCacheDuration { get; set; } = TimeSpan.FromHours(1);

    public bool ValidateLifetime { get; set; } = true;
}

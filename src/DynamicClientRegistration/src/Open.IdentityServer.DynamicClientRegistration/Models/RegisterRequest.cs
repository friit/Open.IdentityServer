using System.Text.Json;
using System.Text.Json.Serialization;

namespace Open.IdentityServer.DynamicClientRegistration.Models;

public class RegisterRequest
{
    // RFC 7591 / OAuth 2.0 Dynamic Client Registration

    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; init; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; init; }

    [JsonPropertyName("grant_types")]
    public string[]? GrantTypes { get; init; }

    [JsonPropertyName("response_types")]
    public string[]? ResponseTypes { get; init; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; init; }

    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; init; }

    [JsonPropertyName("logo_uri")]
    public string? LogoUri { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("contacts")]
    public string[]? Contacts { get; init; }

    [JsonPropertyName("tos_uri")]
    public string? TermsOfServiceUri { get; init; }

    [JsonPropertyName("policy_uri")]
    public string? PolicyUri { get; init; }

    [JsonPropertyName("jwks_uri")]
    public string? JsonWebKeySetUri { get; init; }

    [JsonPropertyName("jwks")]
    public JsonElement? JsonWebKeySet { get; init; }

    [JsonPropertyName("software_id")]
    public string? SoftwareId { get; init; }

    [JsonPropertyName("software_version")]
    public string? SoftwareVersion { get; init; }


    // OpenID Connect Dynamic Client Registration

    [JsonPropertyName("application_type")]
    public string? ApplicationType { get; init; }

    [JsonPropertyName("sector_identifier_uri")]
    public string? SectorIdentifierUri { get; init; }

    [JsonPropertyName("subject_type")]
    public string? SubjectType { get; init; }

    [JsonPropertyName("id_token_signed_response_alg")]
    public string? IdTokenSignedResponseAlgorithm { get; init; }

    [JsonPropertyName("id_token_encrypted_response_alg")]
    public string? IdTokenEncryptedResponseAlgorithm { get; init; }

    [JsonPropertyName("id_token_encrypted_response_enc")]
    public string? IdTokenEncryptedResponseEncryption { get; init; }

    [JsonPropertyName("userinfo_signed_response_alg")]
    public string? UserInfoSignedResponseAlgorithm { get; init; }

    [JsonPropertyName("userinfo_encrypted_response_alg")]
    public string? UserInfoEncryptedResponseAlgorithm { get; init; }

    [JsonPropertyName("userinfo_encrypted_response_enc")]
    public string? UserInfoEncryptedResponseEncryption { get; init; }

    [JsonPropertyName("request_object_signing_alg")]
    public string? RequestObjectSigningAlgorithm { get; init; }

    [JsonPropertyName("request_object_encryption_alg")]
    public string? RequestObjectEncryptionAlgorithm { get; init; }

    [JsonPropertyName("request_object_encryption_enc")]
    public string? RequestObjectEncryption { get; init; }

    [JsonPropertyName("token_endpoint_auth_signing_alg")]
    public string? TokenEndpointAuthSigningAlgorithm { get; init; }

    [JsonPropertyName("default_max_age")]
    public int? DefaultMaxAge { get; init; }

    [JsonPropertyName("require_auth_time")]
    public bool? RequireAuthTime { get; init; }

    [JsonPropertyName("default_acr_values")]
    public string[]? DefaultAcrValues { get; init; }

    [JsonPropertyName("initiate_login_uri")]
    public string? InitiateLoginUri { get; init; }

    [JsonPropertyName("request_uris")]
    public string[]? RequestUris { get; init; }

    [JsonPropertyName("post_logout_redirect_uris")]
    public string[]? PostLogoutRedirectUris { get; init; }


    // RFC 7591 extension metadata / vendor-specific parameters

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalMetadata { get; init; }
    
}
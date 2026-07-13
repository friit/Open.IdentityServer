using System.Text.Json;
using System.Text.Json.Serialization;

namespace Open.IdentityServer.DynamicClientManagement.Models;

public class RegisterRequest
{
    // RFC 7591 / OAuth 2.0 Dynamic Client Management

    /// <summary>
    /// REQUIRED. Redirection URI values used by the client.
    /// </summary>
    /// <remarks>
    /// One registered value must exactly match the <c>redirect_uri</c> parameter used in each authorization request,
    /// using simple string comparison. This field is required by OpenID Connect Dynamic Client Management.
    /// </remarks>
    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; init; }

    /// <summary>
    /// OPTIONAL. Requested client authentication method for the token endpoint.
    /// </summary>
    /// <remarks>
    /// Defined values include <c>client_secret_post</c>, <c>client_secret_basic</c>, <c>client_secret_jwt</c>,
    /// <c>private_key_jwt</c>, and <c>none</c>. Extension authentication methods may also be defined.
    /// If omitted, the default is <c>client_secret_basic</c>.
    /// </remarks>
    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; init; }

    /// <summary>
    /// OPTIONAL. OAuth 2.0 grant types that the client declares it will restrict itself to using.
    /// </summary>
    /// <remarks>
    /// OpenID Connect grant type values include <c>authorization_code</c>, <c>implicit</c>, and
    /// <c>refresh_token</c>. The registered grant types must be compatible with the registered response types.
    /// If omitted, the default is that the client uses only <c>authorization_code</c>.
    /// <list type="bullet">
    /// <item><c>code</c> requires <c>authorization_code</c>.</item>
    /// <item><c>id_token</c> and <c>id_token token</c> require <c>implicit</c>.</item>
    /// <item><c>code id_token</c>, <c>code token</c>, and <c>code id_token token</c> require both <c>authorization_code</c> and <c>implicit</c>.</item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("grant_types")]
    public string[]? GrantTypes { get; init; }

    /// <summary>
    /// OPTIONAL. OAuth 2.0 response type values that the client declares it will restrict itself to using.
    /// </summary>
    /// <remarks>
    /// If omitted, the default is that the client uses only the <c>code</c> response type.
    /// </remarks>
    [JsonPropertyName("response_types")]
    public string[]? ResponseTypes { get; init; }

    /// <summary>
    /// OPTIONAL. Name of the client to be presented to the end user.
    /// </summary>
    /// <remarks>
    /// Localized representations may be supplied using the OpenID Connect client metadata language tag syntax.
    /// </remarks>
    [JsonPropertyName("client_name")]
    public string? ClientName { get; init; }

    /// <summary>
    /// OPTIONAL. URL of the client's home page.
    /// </summary>
    /// <remarks>
    /// The value must point to a valid web page. If present, the server should display it to the end user as a
    /// followable URL. Localized representations may be supplied using the OpenID Connect client metadata language tag syntax.
    /// </remarks>
    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; init; }

    /// <summary>
    /// OPTIONAL. URL that references a logo for the client application.
    /// </summary>
    /// <remarks>
    /// The value must point to a valid image file. If present, the server should display the image to the end user
    /// during approval. Localized representations may be supplied using the OpenID Connect client metadata language tag syntax.
    /// </remarks>
    [JsonPropertyName("logo_uri")]
    public string? LogoUri { get; init; }

    /// <summary>
    /// Space-separated list of OAuth 2.0 scope values.
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    /// <summary>
    /// OPTIONAL. Email addresses of people responsible for this client.
    /// </summary>
    /// <remarks>
    /// Providers may use these contacts to enable management or notification flows for the client.
    /// </remarks>
    [JsonPropertyName("contacts")]
    public string[]? Contacts { get; init; }

    /// <summary>
    /// OPTIONAL. URL that the relying party provides for the end user to read its terms of service.
    /// </summary>
    /// <remarks>
    /// The value must point to a valid web page. If present, the OpenID Provider should display this URL to the end user.
    /// Localized representations may be supplied using the OpenID Connect client metadata language tag syntax.
    /// </remarks>
    [JsonPropertyName("tos_uri")]
    public string? TermsOfServiceUri { get; init; }

    /// <summary>
    /// OPTIONAL. URL that the relying party provides for the end user to read how profile data will be used.
    /// </summary>
    /// <remarks>
    /// The value must point to a valid web page. If present, the OpenID Provider should display this URL to the end user.
    /// Localized representations may be supplied using the OpenID Connect client metadata language tag syntax.
    /// </remarks>
    [JsonPropertyName("policy_uri")]
    public string? PolicyUri { get; init; }

    /// <summary>
    /// OPTIONAL. URL for the client's JSON Web Key Set document.
    /// </summary>
    /// <remarks>
    /// The URL must use the <c>https</c> scheme. The JWK Set contains public signing keys used to validate client
    /// signatures and may also contain public encryption keys used to encrypt responses to the client. When both signing
    /// and encryption keys are available, each key requires a <c>use</c> value indicating its intended public key use.
    /// Using the same key for signing and encryption is not recommended. The JWK Set must not contain private or
    /// symmetric key values. The <c>jwks_uri</c> and <c>jwks</c> parameters must not be used together.
    /// </remarks>
    [JsonPropertyName("jwks_uri")]
    public string? JsonWebKeySetUri { get; init; }

    /// <summary>
    /// OPTIONAL. Client's JSON Web Key Set document, passed by value.
    /// </summary>
    /// <remarks>
    /// This has the same semantics as <c>jwks_uri</c>, except that the JWK Set is included directly rather than
    /// referenced by URL. Clients that can use <c>jwks_uri</c> must not use this parameter. Unlike <c>jwks_uri</c>,
    /// this parameter does not enable key rotation. The JWK Set must not contain private or symmetric key values.
    /// The <c>jwks_uri</c> and <c>jwks</c> parameters must not be used together.
    /// </remarks>
    [JsonPropertyName("jwks")]
    public JsonElement? JsonWebKeySet { get; init; }

    /// <summary>
    /// Identifier for the software that comprises a client.
    /// </summary>
    [JsonPropertyName("software_id")]
    public string? SoftwareId { get; init; }

    /// <summary>
    /// Version identifier for the software that comprises a client.
    /// </summary>
    [JsonPropertyName("software_version")]
    public string? SoftwareVersion { get; init; }
    
    /// <summary>
    /// Software Statement for the software containing Client Metadata. Values in the software statement will override Client Metadata values.
    /// Optionally signed.
    /// </summary>
    [JsonPropertyName("software_statement")]
    public string? SoftwareStatement { get; init; }

    // OpenID Connect Dynamic Client Management

    /// <summary>
    /// OPTIONAL. Kind of application.
    /// </summary>
    /// <remarks>
    /// Defined values are <c>web</c> and <c>native</c>. If omitted, the default is <c>web</c>.
    /// Web clients using the implicit grant type must register only HTTPS redirect URIs and must not use
    /// <c>localhost</c> as the hostname. Native clients must register only custom URI schemes or loopback HTTP URLs
    /// using <c>localhost</c>, <c>127.0.0.1</c>, or <c>[::1]</c>. Authorization servers may reject HTTP redirect
    /// URIs other than loopback URLs for native clients and must verify that registered redirect URIs satisfy these
    /// constraints.
    /// </remarks>
    [JsonPropertyName("application_type")]
    public string? ApplicationType { get; init; }

    /// <summary>
    /// OPTIONAL. HTTPS URL used by the OpenID Provider when calculating pairwise pseudonymous subject identifiers.
    /// </summary>
    /// <remarks>
    /// The URL references a file containing a single JSON array of redirect URI values.
    /// Providers using pairwise subject values should use this value in the pairwise subject identifier calculation.
    /// </remarks>
    [JsonPropertyName("sector_identifier_uri")]
    public string? SectorIdentifierUri { get; init; }

    /// <summary>
    /// OPTIONAL. Requested subject identifier type for responses to this client.
    /// </summary>
    /// <remarks>
    /// Valid values include <c>pairwise</c> and <c>public</c>, subject to the provider's
    /// <c>subject_types_supported</c> discovery metadata.
    /// </remarks>
    [JsonPropertyName("subject_type")]
    public string? SubjectType { get; init; }

    /// <summary>
    /// OPTIONAL. JWS algorithm required for signing ID Tokens issued to this client.
    /// </summary>
    /// <remarks>
    /// The value <c>none</c> must not be used unless the client only uses response types that return no ID Token
    /// from the authorization endpoint. If omitted, the default is <c>RS256</c>.
    /// </remarks>
    [JsonPropertyName("id_token_signed_response_alg")]
    public string? IdTokenSignedResponseAlgorithm { get; init; }

    /// <summary>
    /// OPTIONAL. JWE algorithm required for encrypting ID Tokens issued to this client.
    /// </summary>
    /// <remarks>
    /// When specified, the ID Token is signed and then encrypted as a nested JWT. If omitted, ID Tokens are not encrypted.
    /// </remarks>
    [JsonPropertyName("id_token_encrypted_response_alg")]
    public string? IdTokenEncryptedResponseAlgorithm { get; init; }

    /// <summary>
    /// OPTIONAL. JWE encryption algorithm required for encrypting ID Tokens issued to this client.
    /// </summary>
    /// <remarks>
    /// If <c>id_token_encrypted_response_alg</c> is specified, the default is <c>A128CBC-HS256</c>.
    /// This value must not be included unless <c>id_token_encrypted_response_alg</c> is also provided.
    /// </remarks>
    [JsonPropertyName("id_token_encrypted_response_enc")]
    public string? IdTokenEncryptedResponseEncryption { get; init; }

    /// <summary>
    /// OPTIONAL. JWS algorithm required for signing UserInfo responses.
    /// </summary>
    /// <remarks>
    /// When specified, the UserInfo response is serialized as a JWT and signed using JWS. If omitted, the UserInfo
    /// response returns claims as a UTF-8 encoded JSON object using the <c>application/json</c> content type.
    /// </remarks>
    [JsonPropertyName("userinfo_signed_response_alg")]
    public string? UserInfoSignedResponseAlgorithm { get; init; }

    /// <summary>
    /// OPTIONAL. JWE algorithm required for encrypting UserInfo responses.
    /// </summary>
    /// <remarks>
    /// If both signing and encryption are requested, the UserInfo response is signed and then encrypted as a nested JWT.
    /// If omitted, UserInfo responses are not encrypted.
    /// </remarks>
    [JsonPropertyName("userinfo_encrypted_response_alg")]
    public string? UserInfoEncryptedResponseAlgorithm { get; init; }

    /// <summary>
    /// OPTIONAL. JWE encryption algorithm required for encrypting UserInfo responses.
    /// </summary>
    /// <remarks>
    /// If <c>userinfo_encrypted_response_alg</c> is specified, the default is <c>A128CBC-HS256</c>.
    /// This value must not be included unless <c>userinfo_encrypted_response_alg</c> is also provided.
    /// </remarks>
    [JsonPropertyName("userinfo_encrypted_response_enc")]
    public string? UserInfoEncryptedResponseEncryption { get; init; }

    /// <summary>
    /// OPTIONAL. JWS algorithm that must be used for signing Request Objects sent to the OpenID Provider.
    /// </summary>
    /// <remarks>
    /// Request Objects sent by value using <c>request</c> or by reference using <c>request_uri</c> must be rejected
    /// when not signed with this algorithm. Servers should support <c>RS256</c>. The value <c>none</c> may be used.
    /// If omitted, any algorithm supported by both parties may be used.
    /// </remarks>
    [JsonPropertyName("request_object_signing_alg")]
    public string? RequestObjectSigningAlgorithm { get; init; }

    /// <summary>
    /// OPTIONAL. JWE algorithm the relying party declares it may use for encrypting Request Objects sent to the OpenID Provider.
    /// </summary>
    /// <remarks>
    /// This should be included when symmetric encryption will be used, because it signals that a client secret is needed
    /// to derive the symmetric key. The relying party may still use other supported encryption algorithms or send
    /// unencrypted Request Objects. If both signing and encryption are requested, the Request Object is signed and then
    /// encrypted as a nested JWT. If omitted, the relying party is not declaring whether it might encrypt Request Objects.
    /// </remarks>
    [JsonPropertyName("request_object_encryption_alg")]
    public string? RequestObjectEncryptionAlgorithm { get; init; }

    /// <summary>
    /// OPTIONAL. JWE encryption algorithm the relying party declares it may use for encrypting Request Objects.
    /// </summary>
    /// <remarks>
    /// If <c>request_object_encryption_alg</c> is specified, the default is <c>A128CBC-HS256</c>.
    /// This value must not be included unless <c>request_object_encryption_alg</c> is also provided.
    /// </remarks>
    [JsonPropertyName("request_object_encryption_enc")]
    public string? RequestObjectEncryption { get; init; }

    /// <summary>
    /// OPTIONAL. JWS algorithm that must be used for signing JWTs used to authenticate the client at the token endpoint.
    /// </summary>
    /// <remarks>
    /// Applies to the <c>private_key_jwt</c> and <c>client_secret_jwt</c> authentication methods.
    /// Requests using those methods must be rejected when the JWT is not signed with this algorithm.
    /// Servers should support <c>RS256</c>. The value <c>none</c> must not be used.
    /// If omitted, any algorithm supported by both parties may be used.
    /// </remarks>
    [JsonPropertyName("token_endpoint_auth_signing_alg")]
    public string? TokenEndpointAuthSigningAlgorithm { get; init; }

    /// <summary>
    /// OPTIONAL. Default maximum authentication age, in seconds.
    /// </summary>
    /// <remarks>
    /// Specifies that the end user must be actively authenticated when the previous authentication is older than this
    /// value. The <c>max_age</c> request parameter overrides this value. If omitted, no default maximum authentication
    /// age is specified.
    /// </remarks>
    [JsonPropertyName("default_max_age")]
    public int? DefaultMaxAge { get; init; }

    /// <summary>
    /// OPTIONAL. Indicates whether the <c>auth_time</c> claim is required in ID Tokens.
    /// </summary>
    /// <remarks>
    /// When <see langword="true" />, the <c>auth_time</c> claim is required. If omitted, the default is
    /// <see langword="false" />.
    /// </remarks>
    [JsonPropertyName("require_auth_time")]
    public bool? RequireAuthTime { get; init; }

    /// <summary>
    /// OPTIONAL. Default requested Authentication Context Class Reference values.
    /// </summary>
    /// <remarks>
    /// Values are listed in order of preference and request the <c>acr</c> claim as a voluntary claim. The
    /// authentication context class satisfied by the authentication is returned as the <c>acr</c> claim in the ID Token.
    /// The <c>acr_values</c> request parameter or an individual <c>acr</c> claim request overrides these defaults.
    /// </remarks>
    [JsonPropertyName("default_acr_values")]
    public string[]? DefaultAcrValues { get; init; }

    /// <summary>
    /// OPTIONAL. HTTPS URI that a third party can use to initiate login by the relying party.
    /// </summary>
    /// <remarks>
    /// The URI must accept both GET and POST requests. The client must understand the <c>login_hint</c> and
    /// <c>iss</c> parameters and should support <c>target_link_uri</c>.
    /// </remarks>
    [JsonPropertyName("initiate_login_uri")]
    public string? InitiateLoginUri { get; init; }

    /// <summary>
    /// OPTIONAL. Request URI values pre-registered by the relying party for use at the OpenID Provider.
    /// </summary>
    /// <remarks>
    /// These URLs must use the <c>https</c> scheme unless the target Request Object is signed in a way the provider
    /// can verify. Providers may cache referenced files and may require pre-registration through the
    /// <c>require_request_uri_registration</c> discovery parameter. If a request file can change, the URI should include
    /// the base64url-encoded SHA-256 hash of the file contents as the URI fragment so cache changes can be detected.
    /// </remarks>
    [JsonPropertyName("request_uris")]
    public string[]? RequestUris { get; init; }

    /// <summary>
    /// OPTIONAL. Front-channel logout URI for the client.
    /// </summary>
    /// <remarks>
    /// When supplied, the OpenID Provider can notify the client of logout by loading this URI in the user agent.
    /// </remarks>
    [JsonPropertyName("frontchannel_logout_uri")]
    public string? FrontChannelLogoutUri { get; init; }

    /// <summary>
    /// OPTIONAL. Indicates whether the OP should include the session ID when invoking front-channel logout.
    /// </summary>
    [JsonPropertyName("frontchannel_logout_session_required")]
    public bool? FrontChannelLogoutSessionRequired { get; init; }

    /// <summary>
    /// OPTIONAL. Back-channel logout URI for the client.
    /// </summary>
    /// <remarks>
    /// When supplied, the OpenID Provider can notify the client of logout by sending a logout token to this URI.
    /// </remarks>
    [JsonPropertyName("backchannel_logout_uri")]
    public string? BackChannelLogoutUri { get; init; }

    /// <summary>
    /// OPTIONAL. Indicates whether the OP should include the session ID claim in back-channel logout tokens.
    /// </summary>
    [JsonPropertyName("backchannel_logout_session_required")]
    public bool? BackChannelLogoutSessionRequired { get; init; }

    [JsonPropertyName("post_logout_redirect_uris")]
    public string[]? PostLogoutRedirectUris { get; init; }


    // RFC 7591 extension metadata / vendor-specific parameters

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalMetadata { get; init; }
    
}

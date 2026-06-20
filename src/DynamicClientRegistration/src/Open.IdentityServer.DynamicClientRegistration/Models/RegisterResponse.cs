using System.Text.Json;
using System.Text.Json.Serialization;

namespace Open.IdentityServer.DynamicClientRegistration.Models;

/// <summary>
/// Response from the Dynamic Client Registration endpoint
/// </summary>
public class RegisterResponse
{
    // RFC 7591
    /// <summary>
    /// The client identifier issued by the authorization server.
    /// </summary>
    public required string ClientId { get; init; }
    /// <summary>
    /// The client secret. The client MAY omit the secret.
    /// </summary>
    public string? ClientSecret { get; init; }
    /// <summary>
    /// The time at which the client was issued.
    /// </summary>
    public long? ClientIdIssuedAt { get; init; }
    /// <summary>
    /// If client_secret is issued. Time at which the client_secret will expire or 0 if it will not expire. 
    /// </summary>
    public long? ClientSecretExpiresAt { get; init; }

    // Metadata
    /// <summary>
    /// Specifies the URIs to which the authorization server can redirect the user-agent
    /// after authorization has been granted by the resource owner.
    /// </summary>
    public string[]? RedirectUris { get; init; }
    /// <summary>
    /// Specifies the grant types the client is authorized to use in the OAuth 2.0 authorization process.
    /// </summary>
    public string[]? GrantTypes { get; init; }
    /// <summary>
    /// Specifies the types of responses that the client can use, such as authorization
    /// code, implicit, or token. This corresponds to the "response_types" field defined
    /// in the OAuth 2.0 and OpenID Connect specifications.
    /// </summary>
    public string[]? ResponseTypes { get; init; }
    /// <summary>
    /// Represents the method used by the client to authenticate at the token endpoint.
    /// </summary>
    public string? TokenEndpointAuthMethod { get; init; }

    /// <summary>
    /// The name associated with the client application, used to identify it during registration and further interactions.
    /// </summary>
    public string? ClientName { get; init; }

    /// <summary>
    /// The scope values that the client is authorized to use, represented as a space-delimited string.
    /// </summary>
    public string? Scope { get; init; }
    
    // OIDC
    /// <summary>
    /// The access token provided by the authorization server to manage the client registration details.
    /// </summary>
    public string RegistrationAccessToken { get; init; }

    /// <summary>
    /// The URI where the client can retrieve its dynamic registration information.
    /// </summary>
    public string RegistrationClientUri { get; init; }

    // Extension data
    /// <summary>
    /// Holds additional data not explicitly defined in the response, enabling extensibility for custom properties.
    /// This property is populated with JSON extension data found during deserialization.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; init; }
}
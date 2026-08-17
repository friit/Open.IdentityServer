// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Open.IdentityServer.DynamicClientManagement.Models;

/// <summary>
/// Response from the Dynamic Client Management endpoint
/// </summary>
public record RegisterResponse
{
    // RFC 7591
    /// <summary>
    /// The client identifier issued by the authorization server.
    /// </summary>
    [JsonPropertyName("client_id")]
    public required string ClientId { get; init; }
    
    /// <summary>
    /// The client secret. The client MAY omit the secret.
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; init; }
    
    /// <summary>
    /// The time at which the client was issued.
    /// </summary>
    [JsonPropertyName("client_id_issued_at")]
    public long? ClientIdIssuedAt { get; init; }
    
    /// <summary>
    /// If client_secret is issued. Time at which the client_secret will expire or 0 if it will not expire. 
    /// </summary>
    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; init; }

    // Metadata
    /// <summary>
    /// Specifies the URIs to which the authorization server can redirect the user-agent
    /// after authorization has been granted by the resource owner.
    /// </summary>
    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; init; }
    
    /// <summary>
    /// Specifies the grant types the client is authorized to use in the OAuth 2.0 authorization process.
    /// </summary>
    [JsonPropertyName("grant_types")]
    public string[]? GrantTypes { get; init; }
    
    /// <summary>
    /// Specifies the types of responses that the client can use, such as authorization
    /// code, implicit, or token. This corresponds to the "response_types" field defined
    /// in the OAuth 2.0 and OpenID Connect specifications.
    /// </summary>
    [JsonPropertyName("response_types")]
    public string[]? ResponseTypes { get; init; }
    
    /// <summary>
    /// Represents the method used by the client to authenticate at the token endpoint.
    /// </summary>
    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; init; }

    /// <summary>
    /// The name associated with the client application, used to identify it during registration and further interactions.
    /// </summary>
    [JsonPropertyName("client_name")]
    public string? ClientName { get; init; }

    /// <summary>
    /// The URL of the client's home page.
    /// </summary>
    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; init; }

    /// <summary>
    /// The URL of the client's logo.
    /// </summary>
    [JsonPropertyName("logo_uri")]
    public string? LogoUri { get; init; }
    
    /// <summary>
    /// The scope values that the client is authorized to use, represented as a space-delimited string.
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// Email addresses of people responsible for this client.
    /// </summary>
    [JsonPropertyName("contacts")]
    public string[]? Contacts { get; init; }

    /// <summary>
    /// The client's terms of service URL.
    /// </summary>
    [JsonPropertyName("tos_uri")]
    public string? TermsOfServiceUri { get; init; }

    /// <summary>
    /// The client's policy URL.
    /// </summary>
    [JsonPropertyName("policy_uri")]
    public string? PolicyUri { get; init; }

    /// <summary>
    /// The URL of the client's JSON Web Key Set document.
    /// </summary>
    [JsonPropertyName("jwks_uri")]
    public string? JsonWebKeySetUri { get; init; }

    /// <summary>
    /// The client's JSON Web Key Set document.
    /// </summary>
    [JsonPropertyName("jwks")]
    public JsonElement? JsonWebKeySet { get; init; }

    /// <summary>
    /// The identifier for the software that comprises this client.
    /// </summary>
    [JsonPropertyName("software_id")]
    public string? SoftwareId { get; init; }

    /// <summary>
    /// The version identifier for the software that comprises this client.
    /// </summary>
    [JsonPropertyName("software_version")]
    public string? SoftwareVersion { get; init; }
    
    // OIDC
   
    /// <summary>
    /// The access token provided by the authorization server to manage the client registration details.
    /// </summary>
    [JsonPropertyName("registration_access_token")]
    public string RegistrationAccessToken { get; init; }

    /// <summary>
    /// The URI where the client can retrieve its dynamic registration information.
    /// </summary>
    [JsonPropertyName("registration_client_uri")]
    public string RegistrationClientUri { get; init; }

    // Extension data

    /// <summary>
    /// Holds additional data not explicitly defined in the response, enabling extensibility for custom properties.
    /// This property is populated with JSON extension data found during deserialization.
    /// </summary>x
    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; init; } = new();
}
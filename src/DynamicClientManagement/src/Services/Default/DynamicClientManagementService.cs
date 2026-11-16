// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Exceptions;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Services.Results;
using Open.IdentityServer.DynamicClientManagement.Stores;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Services.Default;

public class DynamicClientManagementService : IDynamicClientManagementService
{
    private const string JsonWebKeySecretType = "JWK";
   
    private readonly DynamicClientManagementOptions _options;
    private readonly IDynamicClientManagementStore _clientStore;
    private readonly ILogger<DynamicClientManagementService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DynamicClientManagementService(
        IDynamicClientManagementStore clientStore, 
        IHttpContextAccessor httpContextAccessor,
        IOptions<DynamicClientManagementOptions> options,
        ILogger<DynamicClientManagementService> logger)
    {
        _clientStore = clientStore;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _logger = logger;
    }
    

    public async Task<DynamicClientManagementClientResult> RegisterAsync(RegisterRequestDTO requestDto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Defaults to client_secret_basic if not specified according to the spec.
        var tokenEndpointAuthMethod = requestDto.TokenEndpointAuthMethod ?? "client_secret_basic";

        // Generate client id and secret.
        var generatedClientId = GenerateClientId();
        var generatedClientSecret = GenerateClientSecret(tokenEndpointAuthMethod);

        // Defaults to authorization_code if not specified according to the spec.
        var grantTypes = requestDto.GrantTypes ?? ["authorization_code"];

        var scopes = requestDto.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var client = new Client()
        {
            ClientId = generatedClientId,
            ClientName = requestDto.ClientName,
            ClientUri = requestDto.ClientUri,
            LogoUri = requestDto.LogoUri,
            RedirectUris = requestDto.RedirectUris,
            PostLogoutRedirectUris = requestDto.PostLogoutRedirectUris,
            AllowedGrantTypes = grantTypes,
            AllowedScopes = scopes,
            ClientSecrets = CreateClientSecrets(requestDto, tokenEndpointAuthMethod, generatedClientSecret),
            RequireClientSecret = tokenEndpointAuthMethod != "none",
            RequirePkce = grantTypes.Contains("authorization_code"),
            AllowOfflineAccess = scopes?.Contains("offline_access") == true || grantTypes.Contains("refresh_token"),
            FrontChannelLogoutUri = requestDto.FrontChannelLogoutUri,
            BackChannelLogoutUri = requestDto.BackChannelLogoutUri,
            UserSsoLifetime = requestDto.UserSsoLifetime ?? requestDto.DefaultMaxAge,
            InitiateLoginUri = requestDto.InitiateLoginUri,
            Claims = requestDto.ClientClaims?
                .Select(claim => new ClientClaim(claim.Key, claim.Value.ToString()))
                .ToList()
        };

        ApplyOpenIdentityServerParameters(requestDto, client);
        client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + "token_endpoint_auth_method"] =
            tokenEndpointAuthMethod;

        if(requestDto.Contacts != null)
        {
            client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + "contacts"] = JsonSerializer.Serialize(requestDto.Contacts);
        }
        
        if(requestDto.PolicyUri != null)
        {
            client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + "policy_uri"] = requestDto.PolicyUri;
        }
        
        if(requestDto.SoftwareId != null)
        {
            client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + "software_id"] = requestDto.SoftwareId;
        }
        
        if(requestDto.SoftwareVersion != null)
        {
            client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + "software_version"] = requestDto.SoftwareVersion;
        }
        
        if(requestDto.TermsOfServiceUri != null)
        {
            client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + "tos_uri"] = requestDto.TermsOfServiceUri;
        }
        
        if (requestDto.JsonWebKeySetUri != null)
        {
            client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + "jwks_uri"] = requestDto.JsonWebKeySetUri;
        }

        if (requestDto.JsonWebKeySet.HasValue)
        {
            client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + "jwks"] = requestDto.JsonWebKeySet.Value.GetRawText();
        }

        foreach (var metadata in requestDto.AdditionalMetadata ?? [])
        {
            client.Properties[Constants.DynamicClientManagementProperties.InternalPropertyPrefix + metadata.Key] = metadata.Value.GetRawText();
        }

        var token = GenerateToken(client, requestDto);
        
        //TODO: review this, should we store the token in the client properties? or should we store it in a separate store?
        // Should this also store actual data?
        client.Properties[Constants.DynamicClientManagementProperties.DynamicClientManagementTokenHashProperty] = token.hash;
        
        await _clientStore.RegisterClientAsync(client, cancellationToken);
        

        _logger.LogDebug(
            "Registered dynamic client {clientId} using token endpoint auth method {tokenEndpointAuthMethod}.",
            generatedClientId,
            tokenEndpointAuthMethod);

        return new DynamicClientManagementClientResult
        {
            Client = client,
            ClientSecret = generatedClientSecret,
            RegistrationAccessToken = token.raw,
            RegistrationClientUri = GetRegistrationClientUri(generatedClientId),
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            //Todo: Implement proper expiration for client secrets if needed. For now, we set it to UnixEpoch if a secret is generated, otherwise null.
            ClientSecretExpiresAt = generatedClientSecret is null ? null : DateTimeOffset.UnixEpoch,
            OriginalRequestDto = requestDto,
        };
    }
    

    private string GetRegistrationClientUri(string clientId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var registerPath = _options.RegisterPath;
        
        var uriBuilder = new UriBuilder(
            httpContext.Request.Scheme,
            httpContext.Request.Host.Host,
            httpContext.Request.Host.Port ?? -1)
        {
            Path = $"{registerPath}/{clientId}"
        };

        return uriBuilder.ToString();
    }

    protected virtual (string raw, string hash) GenerateToken(Client client, RegisterRequestDTO requestDto)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);
        var tokenHash = Convert.ToBase64String(SHA256.HashData(tokenBytes));
        
        return (token, tokenHash);
    }

    private static void ApplyOpenIdentityServerParameters(RegisterRequestDTO requestDto, Client client)
    {
        if (requestDto.Enabled.HasValue)
        {
            client.Enabled = requestDto.Enabled.Value;
        }

        if (requestDto.RequireConsent.HasValue)
        {
            client.RequireConsent = requestDto.RequireConsent.Value;
        }

        if (requestDto.AllowRememberConsent.HasValue)
        {
            client.AllowRememberConsent = requestDto.AllowRememberConsent.Value;
        }

        if (requestDto.RequirePkce.HasValue)
        {
            client.RequirePkce = requestDto.RequirePkce.Value;
        }

        if (requestDto.RequireRequestObject.HasValue)
        {
            client.RequireRequestObject = requestDto.RequireRequestObject.Value;
        }

        if (requestDto.AlwaysIncludeUserClaimsInIdToken.HasValue)
        {
            client.AlwaysIncludeUserClaimsInIdToken = requestDto.AlwaysIncludeUserClaimsInIdToken.Value;
        }

        if (requestDto.IdentityTokenLifetime.HasValue)
        {
            client.IdentityTokenLifetime = requestDto.IdentityTokenLifetime.Value;
        }

        if (requestDto.AccessTokenLifetime.HasValue)
        {
            client.AccessTokenLifetime = requestDto.AccessTokenLifetime.Value;
        }

        if (requestDto.AuthorizationCodeLifetime.HasValue)
        {
            client.AuthorizationCodeLifetime = requestDto.AuthorizationCodeLifetime.Value;
        }

        if (requestDto.AbsoluteRefreshTokenLifetime.HasValue)
        {
            client.AbsoluteRefreshTokenLifetime = requestDto.AbsoluteRefreshTokenLifetime.Value;
        }

        if (requestDto.SlidingRefreshTokenLifetime.HasValue)
        {
            client.SlidingRefreshTokenLifetime = requestDto.SlidingRefreshTokenLifetime.Value;
        }

        if (requestDto.ConsentLifetime.HasValue)
        {
            client.ConsentLifetime = requestDto.ConsentLifetime.Value;
        }

        if (requestDto.RefreshTokenUsage is not null)
        {
            client.RefreshTokenUsage = ParseEnum<TokenUsage>(
                requestDto.RefreshTokenUsage,
                nameof(requestDto.RefreshTokenUsage));
        }

        if (requestDto.UpdateAccessTokenClaimsOnRefresh.HasValue)
        {
            client.UpdateAccessTokenClaimsOnRefresh = requestDto.UpdateAccessTokenClaimsOnRefresh.Value;
        }

        if (requestDto.RefreshTokenExpiration is not null)
        {
            client.RefreshTokenExpiration = ParseEnum<TokenExpiration>(
                requestDto.RefreshTokenExpiration,
                nameof(requestDto.RefreshTokenExpiration));
        }

        if (requestDto.AccessTokenType is not null)
        {
            client.AccessTokenType = ParseEnum<AccessTokenType>(
                requestDto.AccessTokenType,
                nameof(requestDto.AccessTokenType));
        }

        if (requestDto.EnableLocalLogin.HasValue)
        {
            client.EnableLocalLogin = requestDto.EnableLocalLogin.Value;
        }

        if (requestDto.IdentityProviderRestrictions is not null)
        {
            client.IdentityProviderRestrictions = requestDto.IdentityProviderRestrictions;
        }

        if (requestDto.IncludeJwtId.HasValue)
        {
            client.IncludeJwtId = requestDto.IncludeJwtId.Value;
        }

        if (requestDto.ClientClaims is not null)
        {
            client.Claims = requestDto.ClientClaims
                .Select(claim => new ClientClaim(claim.Key, claim.Value.ToString()))
                .ToList();
        }

        if (requestDto.ClientClaimsPrefix is not null)
        {
            client.ClientClaimsPrefix = requestDto.ClientClaimsPrefix;
        }

        if (requestDto.PairwiseSubjectSalt is not null)
        {
            client.PairWiseSubjectSalt = requestDto.PairwiseSubjectSalt;
        }

        if (requestDto.UserSsoLifetime.HasValue)
        {
            client.UserSsoLifetime = requestDto.UserSsoLifetime.Value;
        }

        if (requestDto.UserCodeType is not null)
        {
            client.UserCodeType = requestDto.UserCodeType;
        }

        if (requestDto.DeviceCodeLifetime.HasValue)
        {
            client.DeviceCodeLifetime = requestDto.DeviceCodeLifetime.Value;
        }

        if (requestDto.AllowedCorsOrigins is not null)
        {
            client.AllowedCorsOrigins = requestDto.AllowedCorsOrigins;
        }

        if (requestDto.FrontChannelLogoutSessionRequired.HasValue)
        {
            client.FrontChannelLogoutSessionRequired = requestDto.FrontChannelLogoutSessionRequired.Value;
        }

        if (requestDto.BackChannelLogoutSessionRequired.HasValue)
        {
            client.BackChannelLogoutSessionRequired = requestDto.BackChannelLogoutSessionRequired.Value;
        }

        if (requestDto.AllowAccessTokensViaBrowser.HasValue)
        {
            client.AllowAccessTokensViaBrowser = requestDto.AllowAccessTokensViaBrowser.Value;
        }

    }

    private static TEnum ParseEnum<TEnum>(string value, string propertyName)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse(value, ignoreCase: true, out TEnum parsed) ||
            Enum.TryParse(
                value.Replace("_", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal),
                ignoreCase: true,
                out parsed))
        {
            return parsed;
        }

        throw new InvalidDynamicClientManagementRequestException(
            Constants.RegisterErrors.InvalidClientMetadata,
            $"{propertyName} '{value}' is not a valid {typeof(TEnum).Name} value.");
    }
    
    protected virtual string GenerateClientId()
    {
        return Guid.NewGuid().ToString();
    }
    
    protected virtual string? GenerateClientSecret(string tokenEndpointAuthMethod)
    {
        
        return tokenEndpointAuthMethod switch{
            "none" => null,
            "client_secret_basic" or "client_secret_post" => Guid.NewGuid().ToString(),
            "private_key_jwt" => null,
            _ => throw new InvalidDynamicClientManagementRequestException(
                Constants.RegisterErrors.InvalidClientMetadata,
                $"token_endpoint_auth_method '{tokenEndpointAuthMethod}' is not supported.")
        };
    }

    protected virtual ICollection<Secret> CreateClientSecrets(
        RegisterRequestDTO requestDto,
        string tokenEndpointAuthMethod,
        string? generatedClientSecret)
    {
        return tokenEndpointAuthMethod switch
        {
            "none" => [],
            "client_secret_basic" or "client_secret_post" when generatedClientSecret != null =>
                [new Secret(HashSharedSecret(generatedClientSecret))],
            "private_key_jwt" => CreateJwkSecrets(requestDto),
            _ => throw new InvalidDynamicClientManagementRequestException(
                Constants.RegisterErrors.InvalidClientMetadata,
                $"token_endpoint_auth_method '{tokenEndpointAuthMethod}' is not supported.")
        };
    }

    protected virtual string HashSharedSecret(string sharedSecret)
    {
        var bytes = Encoding.UTF8.GetBytes(sharedSecret);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }

    protected virtual ICollection<Secret> CreateJwkSecrets(RegisterRequestDTO requestDto)
    {
        if (requestDto.JsonWebKeySet is null || requestDto.JsonWebKeySet.Value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDynamicClientManagementRequestException(
                Constants.RegisterErrors.InvalidClientMetadata,
                "jwks must be provided for token_endpoint_auth_method 'private_key_jwt'.");
        }

        if (!requestDto.JsonWebKeySet.Value.TryGetProperty("keys", out var keysElement) ||
            keysElement.ValueKind != JsonValueKind.Array ||
            keysElement.GetArrayLength() == 0)
        {
            throw new InvalidDynamicClientManagementRequestException(
                Constants.RegisterErrors.InvalidClientMetadata,
                "jwks must contain at least one key for token_endpoint_auth_method 'private_key_jwt'.");
        }

        return keysElement.EnumerateArray()
            .Select(key => new Secret
            {
                Type = JsonWebKeySecretType,
                Value = key.GetRawText()
            })
            .ToList();
    }
}
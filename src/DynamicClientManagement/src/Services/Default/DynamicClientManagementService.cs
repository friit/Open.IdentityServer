// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Open.IdentityServer.DynamicClientManagement.Exceptions;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;
using Open.IdentityServer.DynamicClientManagement.Stores;
using Open.IdentityServer.Models;
using Open.IdentityServer.Stores;

namespace Open.IdentityServer.DynamicClientManagement.Services.Default;

public class DynamicClientManagementService : IDynamicClientManagementService
{
    private const string JsonWebKeySecretType = "JWK";

    private readonly IDynamicClientManagementStore _clientStore;
    private readonly ILogger<DynamicClientManagementService> _logger;

    public DynamicClientManagementService(IDynamicClientManagementStore clientStore, ILogger<DynamicClientManagementService> logger)
    {
        _clientStore = clientStore;
        _logger = logger;
    }

    public async Task<DynamicClientManagementResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Defaults to client_secret_basic if not specified according to the spec.
        var tokenEndpointAuthMethod = request.TokenEndpointAuthMethod ?? "client_secret_basic";

        // Generate client id and secret.
        var generatedClientId = GenerateClientId();
        var generatedClientSecret = GenerateClientSecret(tokenEndpointAuthMethod);

        // Defaults to authorization_code if not specified according to the spec.
        var grantTypes = request.GrantTypes ?? ["authorization_code"];

        var scopes = request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var client = new Client()
        {
            ClientId = generatedClientId,
            ClientName = request.ClientName,
            ClientUri = request.ClientUri,
            LogoUri = request.LogoUri,
            RedirectUris = request.RedirectUris,
            PostLogoutRedirectUris = request.PostLogoutRedirectUris,
            AllowedGrantTypes = grantTypes,
            AllowedScopes = scopes,
            ClientSecrets = CreateClientSecrets(request, tokenEndpointAuthMethod, generatedClientSecret),
            RequireClientSecret = tokenEndpointAuthMethod != "none",
            RequirePkce = grantTypes.Contains("authorization_code"),
            AllowOfflineAccess = scopes?.Contains("offline_access") == true || grantTypes.Contains("refresh_token"),
            FrontChannelLogoutUri = request.FrontChannelLogoutUri,
            BackChannelLogoutUri = request.BackChannelLogoutUri,
            UserSsoLifetime = request.UserSsoLifetime ?? request.DefaultMaxAge,
            InitiateLoginUri = request.InitiateLoginUri,
        };

        ApplyOpenIdentityServerParameters(request, client);

        if(request.Contacts != null)
        {
            client.Properties["contacts"] = JsonSerializer.Serialize(request.Contacts);
        }
        
        if(request.TermsOfServiceUri != null)
        {
            client.Properties["terms_of_service_uri"] = request.TermsOfServiceUri;
        }
        
        if(request.PolicyUri != null)
        {
            client.Properties["policy_uri"] = request.PolicyUri;
        }
        
        if(request.SoftwareId != null)
        {
            client.Properties["software_id"] = request.SoftwareId;
        }
        
        if(request.SoftwareVersion != null)
        {
            client.Properties["software_version"] = request.SoftwareVersion;
        }

        var token = GenerateToken(client, request);
        
        //TODO: review this, should we store the token in the client properties? or should we store it in a separate store?
        // Should this also store actual data?
        client.Properties["dcm_token_hash"] = token.hash;
        
        await _clientStore.RegisterClientAsync(client, cancellationToken);
        

        _logger.LogDebug(
            "Registered dynamic client {clientId} using token endpoint auth method {tokenEndpointAuthMethod}.",
            generatedClientId,
            tokenEndpointAuthMethod);

        return new DynamicClientManagementResult
        {
            Client = client,
            ClientSecret = generatedClientSecret,
            RegistrationAccessToken = token.raw,
            RegistrationClientUri = $"connect/register/{generatedClientId}",
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            //Todo: Implement proper expiration for client secrets if needed. For now, we set it to UnixEpoch if a secret is generated, otherwise null.
            ClientSecretExpiresAt = generatedClientSecret is null ? null : DateTimeOffset.UnixEpoch,
            OriginalRequest = request,
        };
    }

    protected virtual (string raw, string hash) GenerateToken(Client client, RegisterRequest request)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);
        var tokenHash = Convert.ToBase64String(SHA256.HashData(tokenBytes));
        
        return (token, tokenHash);
    }

    private static void ApplyOpenIdentityServerParameters(RegisterRequest request, Client client)
    {
        if (request.Enabled.HasValue)
        {
            client.Enabled = request.Enabled.Value;
        }

        if (request.RequireConsent.HasValue)
        {
            client.RequireConsent = request.RequireConsent.Value;
        }

        if (request.AllowRememberConsent.HasValue)
        {
            client.AllowRememberConsent = request.AllowRememberConsent.Value;
        }

        if (request.RequirePkce.HasValue)
        {
            client.RequirePkce = request.RequirePkce.Value;
        }

        if (request.RequireRequestObject.HasValue)
        {
            client.RequireRequestObject = request.RequireRequestObject.Value;
        }

        if (request.AlwaysIncludeUserClaimsInIdToken.HasValue)
        {
            client.AlwaysIncludeUserClaimsInIdToken = request.AlwaysIncludeUserClaimsInIdToken.Value;
        }

        if (request.IdentityTokenLifetime.HasValue)
        {
            client.IdentityTokenLifetime = request.IdentityTokenLifetime.Value;
        }

        if (request.AccessTokenLifetime.HasValue)
        {
            client.AccessTokenLifetime = request.AccessTokenLifetime.Value;
        }

        if (request.AuthorizationCodeLifetime.HasValue)
        {
            client.AuthorizationCodeLifetime = request.AuthorizationCodeLifetime.Value;
        }

        if (request.AbsoluteRefreshTokenLifetime.HasValue)
        {
            client.AbsoluteRefreshTokenLifetime = request.AbsoluteRefreshTokenLifetime.Value;
        }

        if (request.SlidingRefreshTokenLifetime.HasValue)
        {
            client.SlidingRefreshTokenLifetime = request.SlidingRefreshTokenLifetime.Value;
        }

        if (request.ConsentLifetime.HasValue)
        {
            client.ConsentLifetime = request.ConsentLifetime.Value;
        }

        if (request.RefreshTokenUsage is not null)
        {
            client.RefreshTokenUsage = ParseEnum<TokenUsage>(
                request.RefreshTokenUsage,
                nameof(request.RefreshTokenUsage));
        }

        if (request.UpdateAccessTokenClaimsOnRefresh.HasValue)
        {
            client.UpdateAccessTokenClaimsOnRefresh = request.UpdateAccessTokenClaimsOnRefresh.Value;
        }

        if (request.RefreshTokenExpiration is not null)
        {
            client.RefreshTokenExpiration = ParseEnum<TokenExpiration>(
                request.RefreshTokenExpiration,
                nameof(request.RefreshTokenExpiration));
        }

        if (request.AccessTokenType is not null)
        {
            client.AccessTokenType = ParseEnum<AccessTokenType>(
                request.AccessTokenType,
                nameof(request.AccessTokenType));
        }

        if (request.EnableLocalLogin.HasValue)
        {
            client.EnableLocalLogin = request.EnableLocalLogin.Value;
        }

        if (request.IdentityProviderRestrictions is not null)
        {
            client.IdentityProviderRestrictions = request.IdentityProviderRestrictions;
        }

        if (request.IncludeJwtId.HasValue)
        {
            client.IncludeJwtId = request.IncludeJwtId.Value;
        }

        if (request.ClientClaims is not null)
        {
            client.Claims = request.ClientClaims
                .Select(claim => new ClientClaim(claim.Key, claim.Value.ToString()))
                .ToList();
        }

        if (request.ClientClaimsPrefix is not null)
        {
            client.ClientClaimsPrefix = request.ClientClaimsPrefix;
        }

        if (request.PairwiseSubjectSalt is not null)
        {
            client.PairWiseSubjectSalt = request.PairwiseSubjectSalt;
        }

        if (request.UserSsoLifetime.HasValue)
        {
            client.UserSsoLifetime = request.UserSsoLifetime.Value;
        }

        if (request.UserCodeType is not null)
        {
            client.UserCodeType = request.UserCodeType;
        }

        if (request.DeviceCodeLifetime.HasValue)
        {
            client.DeviceCodeLifetime = request.DeviceCodeLifetime.Value;
        }

        if (request.AllowedCorsOrigins is not null)
        {
            client.AllowedCorsOrigins = request.AllowedCorsOrigins;
        }

        if (request.FrontChannelLogoutSessionRequired.HasValue)
        {
            client.FrontChannelLogoutSessionRequired = request.FrontChannelLogoutSessionRequired.Value;
        }

        if (request.BackChannelLogoutSessionRequired.HasValue)
        {
            client.BackChannelLogoutSessionRequired = request.BackChannelLogoutSessionRequired.Value;
        }

        if (request.AllowAccessTokensViaBrowser.HasValue)
        {
            client.AllowAccessTokensViaBrowser = request.AllowAccessTokensViaBrowser.Value;
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
        RegisterRequest request,
        string tokenEndpointAuthMethod,
        string? generatedClientSecret)
    {
        return tokenEndpointAuthMethod switch
        {
            "none" => [],
            "client_secret_basic" or "client_secret_post" when generatedClientSecret != null =>
                [new Secret(HashSharedSecret(generatedClientSecret))],
            "private_key_jwt" => CreateJwkSecrets(request),
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

    protected virtual ICollection<Secret> CreateJwkSecrets(RegisterRequest request)
    {
        if (request.JsonWebKeySet is null || request.JsonWebKeySet.Value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDynamicClientManagementRequestException(
                Constants.RegisterErrors.InvalidClientMetadata,
                "jwks must be provided for token_endpoint_auth_method 'private_key_jwt'.");
        }

        if (!request.JsonWebKeySet.Value.TryGetProperty("keys", out var keysElement) ||
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
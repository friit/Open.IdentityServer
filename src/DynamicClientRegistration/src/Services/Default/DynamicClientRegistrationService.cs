using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Open.IdentityServer.DynamicClientRegistration.Exceptions;
using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Models.Results;
using Open.IdentityServer.Models;
using Open.IdentityServer.Stores;

namespace Open.IdentityServer.DynamicClientRegistration.Services.Default;

public class DynamicClientRegistrationService : IDynamicClientRegistrationService
{
    private const string JsonWebKeySecretType = "JWK";

    private readonly IClientStore _clientStore;
    private readonly ILogger<DynamicClientRegistrationService> _logger;

    public DynamicClientRegistrationService(IClientStore clientStore, ILogger<DynamicClientRegistrationService> logger)
    {
        _clientStore = clientStore;
        _logger = logger;
    }

    public Task<DynamicClientRegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var generatedClientId = Guid.NewGuid().ToString();
        var scopes = request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var grantTypes = request.GrantTypes ?? ["authorization_code"];
        var tokenEndpointAuthMethod = request.TokenEndpointAuthMethod ?? "client_secret_basic";
        var generatedClientSecret = tokenEndpointAuthMethod is "client_secret_basic" or "client_secret_post"
            ? Guid.NewGuid().ToString()
            : null;
        var clientSecrets = CreateClientSecrets(request, tokenEndpointAuthMethod, generatedClientSecret);

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
            ClientSecrets = clientSecrets,
            RequireClientSecret = tokenEndpointAuthMethod != "none",
            RequirePkce = grantTypes.Contains("authorization_code"),
            AllowOfflineAccess = scopes?.Contains("offline_access") == true,
            AllowAccessTokensViaBrowser = grantTypes.Contains("implicit"),
            FrontChannelLogoutUri = request.FrontChannelLogoutUri,
            FrontChannelLogoutSessionRequired = request.FrontChannelLogoutSessionRequired ?? true,
            BackChannelLogoutUri = request.BackChannelLogoutUri,
            BackChannelLogoutSessionRequired = request.BackChannelLogoutSessionRequired ?? true,
            UserSsoLifetime = request.DefaultMaxAge,
            InitiateLoginUri = request.InitiateLoginUri,
            
        };
        
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

        _logger.LogDebug(
            "Registered dynamic client {clientId} using token endpoint auth method {tokenEndpointAuthMethod}.",
            generatedClientId,
            tokenEndpointAuthMethod);

        return Task.FromResult(new DynamicClientRegistrationResult
        {
            Client = client,
            ClientSecret = generatedClientSecret,
            RegistrationAccessToken = Guid.NewGuid().ToString("N"),
            RegistrationClientUri = $"connect/register/{generatedClientId}",
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            ClientSecretExpiresAt = generatedClientSecret is null ? null : DateTimeOffset.UnixEpoch,
            EffectiveRequest = request
        });
    }

    private static ICollection<Secret> CreateClientSecrets(
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
            _ => throw new InvalidDynamicClientRegistrationRequestException(
                Constants.RegisterErrors.InvalidClientMetadata,
                $"token_endpoint_auth_method '{tokenEndpointAuthMethod}' is not supported.")
        };
    }

    private static string HashSharedSecret(string sharedSecret)
    {
        var bytes = Encoding.UTF8.GetBytes(sharedSecret);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }

    private static ICollection<Secret> CreateJwkSecrets(RegisterRequest request)
    {
        if (request.JsonWebKeySet is null || request.JsonWebKeySet.Value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDynamicClientRegistrationRequestException(
                Constants.RegisterErrors.InvalidClientMetadata,
                "jwks must be provided for token_endpoint_auth_method 'private_key_jwt'.");
        }

        if (!request.JsonWebKeySet.Value.TryGetProperty("keys", out var keysElement) ||
            keysElement.ValueKind != JsonValueKind.Array ||
            keysElement.GetArrayLength() == 0)
        {
            throw new InvalidDynamicClientRegistrationRequestException(
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
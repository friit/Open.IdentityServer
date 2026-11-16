using System.Text.Json;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Services.Results;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Generators.Default;

public class DynamicClientManagementResponseGenerator : IDynamicClientManagementResponseGenerator
{
    private const string InternalPropertyPrefix = "__dcm_property:";

    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.Ordinal)
    {
        "dcm_token_hash",
        "contacts",
        "tos_uri",
        "policy_uri",
        "jwks_uri",
        "jwks",
        "token_endpoint_auth_method",
        "software_id",
        "software_version"
    };

    public Task<ClientResponseDTO> GenerateGetResponseAsync(Client client, CancellationToken cancellationToken = default)
    {
         cancellationToken.ThrowIfCancellationRequested();
    
        var response = new ClientResponseDTO
        {
            ClientName = client.ClientName,
            ClientId = client.ClientId,
            RedirectUris = client.RedirectUris?.ToArray(),
            GrantTypes = client.AllowedGrantTypes?.ToArray(),
            TokenEndpointAuthMethod = GetProperty(client, "token_endpoint_auth_method"),
            ClientUri = client.ClientUri,
            LogoUri = client.LogoUri,
            Contacts = GetContacts(client),
            TermsOfServiceUri = GetProperty(client, "tos_uri"),
            PolicyUri = GetProperty(client, "policy_uri"),
            JsonWebKeySetUri = GetProperty(client, "jwks_uri"),
            JsonWebKeySet = GetJsonProperty(client, "jwks"),
            SoftwareId = GetProperty(client, "software_id"),
            SoftwareVersion = GetProperty(client, "software_version"),
            
        };

        if (client.AllowedScopes != null && client.AllowedScopes.Any())
        {
            response.Scope = string.Join(" ", client.AllowedScopes);
        }

        response.AdditionalData["enabled"] = JsonSerializer.SerializeToElement(client.Enabled);
        
        var additionalData = ExtractAdditionalData(client);

        foreach (var property in additionalData)
        {
            response.AdditionalData[property.Name] = ParseJsonProperty(property.Name, property.Value);
        }

        return Task.FromResult(response);

    }

    public Task<ClientResponseDTO> GenerateRegisterResponseAsync(DynamicClientManagementClientResult registrationClientResult, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
    
        var response = new ClientResponseDTO
        {
            ClientName = registrationClientResult.Client.ClientName,
            ClientId = registrationClientResult.Client.ClientId,
            ClientSecret = registrationClientResult.ClientSecret,
            ClientIdIssuedAt = registrationClientResult.ClientIdIssuedAt.ToUnixTimeSeconds(),
            ClientSecretExpiresAt = registrationClientResult.ClientSecretExpiresAt?.ToUnixTimeSeconds(),
            RegistrationAccessToken = registrationClientResult.RegistrationAccessToken,
            RegistrationClientUri = registrationClientResult.RegistrationClientUri,
            RedirectUris = registrationClientResult.Client.RedirectUris?.ToArray(),
            GrantTypes = registrationClientResult.Client.AllowedGrantTypes?.ToArray(),
            //TODO: Better way to presist this? techincally the default here is fragile
            TokenEndpointAuthMethod = registrationClientResult.OriginalRequestDto.TokenEndpointAuthMethod,
            ResponseTypes = registrationClientResult.OriginalRequestDto.ResponseTypes?.ToArray(),
            ClientUri = registrationClientResult.Client.ClientUri,
            LogoUri = registrationClientResult.Client.LogoUri,
            Contacts = GetContacts(registrationClientResult.Client),
            TermsOfServiceUri = GetProperty(registrationClientResult.Client, "tos_uri"),
            PolicyUri = GetProperty(registrationClientResult.Client, "policy_uri"),
            JsonWebKeySetUri = GetProperty(registrationClientResult.Client, "jwks_uri"),
            JsonWebKeySet = GetJsonProperty(registrationClientResult.Client, "jwks"),
            SoftwareId = GetProperty(registrationClientResult.Client, "software_id"),
            SoftwareVersion = GetProperty(registrationClientResult.Client, "software_version"),
            
        };

        if (registrationClientResult.Client.AllowedScopes != null && registrationClientResult.Client.AllowedScopes.Any())
        {
            response.Scope = string.Join(" ", registrationClientResult.Client.AllowedScopes);
        }
    
        if (registrationClientResult.OriginalRequestDto.Enabled.HasValue)
        {
            response.AdditionalData["enabled"] = JsonSerializer.SerializeToElement(registrationClientResult.Client.Enabled);
        }
        
        var additionalData = ExtractAdditionalData(registrationClientResult.Client);
        
        foreach (var property in additionalData)
        {
            response.AdditionalData[property.Name] = ParseJsonProperty(property.Name, property.Value);
        }
        
        return Task.FromResult(response);
    }
    
    private static IEnumerable<(string Name, string Value)> ExtractAdditionalData(Client client)
    {
        var additionalData = client.Properties
            .Where(property => property.Key.StartsWith(InternalPropertyPrefix, StringComparison.Ordinal))
            .Select(property => (Name: property.Key[InternalPropertyPrefix.Length..], Value: property.Value))
            .Where(property => !IgnoredProperties.Contains(property.Name));
        
        return additionalData;
    }

    private static JsonElement ParseJsonProperty(string propertyName, string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Client property '{propertyName}' must contain valid JSON metadata.",
                ex);
        }
    }

    private static string? GetProperty(Client client, string propertyName)
    {
        return client.Properties.TryGetValue(InternalPropertyPrefix + propertyName, out var value)
            ? value
            : null;
    }

    private static string[]? GetContacts(Client client)
    {
        if (!client.Properties.TryGetValue(InternalPropertyPrefix + "contacts", out var value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(value);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Client property 'contacts' must contain a valid JSON string array.",
                ex);
        }
    }

    private static JsonElement? GetJsonProperty(Client client, string propertyName)
    {
        if (!client.Properties.TryGetValue(propertyName, out var value))
        {
            return null;
        }

        return ParseJsonProperty(propertyName, value);
    }
}
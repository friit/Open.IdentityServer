using System.Text.Json;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Services.Results;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Generators.Default;

public class DynamicClientManagementResponseGenerator : IDynamicClientManagementResponseGenerator
{
    // These properties are stored in Client.Properties but are mapped explicitly or kept internal
    // rather than returned as additional metadata.
    private static readonly HashSet<string> ExplicitlyMappedProperties = new(StringComparer.Ordinal)
    {
        Constants.DynamicClientManagementProperties.TokenHash,
        Constants.ClientMetadata.Contacts,
        Constants.ClientMetadata.TermsOfServiceUri,
        Constants.ClientMetadata.PolicyUri,
        Constants.ClientMetadata.JsonWebKeySetUri,
        Constants.ClientMetadata.JsonWebKeySet,
        Constants.ClientMetadata.TokenEndpointAuthMethod,
        Constants.ClientMetadata.SoftwareId,
        Constants.ClientMetadata.SoftwareVersion
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
            TokenEndpointAuthMethod = GetProperty(client, Constants.ClientMetadata.TokenEndpointAuthMethod),
            ClientUri = client.ClientUri,
            LogoUri = client.LogoUri,
            Contacts = GetContacts(client),
            TermsOfServiceUri = GetProperty(client, Constants.ClientMetadata.TermsOfServiceUri),
            PolicyUri = GetProperty(client, Constants.ClientMetadata.PolicyUri),
            JsonWebKeySetUri = GetProperty(client, Constants.ClientMetadata.JsonWebKeySetUri),
            JsonWebKeySet = GetJsonProperty(client, Constants.ClientMetadata.JsonWebKeySet),
            SoftwareId = GetProperty(client, Constants.ClientMetadata.SoftwareId),
            SoftwareVersion = GetProperty(client, Constants.ClientMetadata.SoftwareVersion),
            
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
            TermsOfServiceUri = GetProperty(registrationClientResult.Client, Constants.ClientMetadata.TermsOfServiceUri),
            PolicyUri = GetProperty(registrationClientResult.Client, Constants.ClientMetadata.PolicyUri),
            JsonWebKeySetUri = GetProperty(registrationClientResult.Client, Constants.ClientMetadata.JsonWebKeySetUri),
            JsonWebKeySet = GetJsonProperty(registrationClientResult.Client, Constants.ClientMetadata.JsonWebKeySet),
            SoftwareId = GetProperty(registrationClientResult.Client, Constants.ClientMetadata.SoftwareId),
            SoftwareVersion = GetProperty(registrationClientResult.Client, Constants.ClientMetadata.SoftwareVersion),
            
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
            .Where(property => property.Key.StartsWith(
                Constants.DynamicClientManagementProperties.InternalPropertyPrefix,
                StringComparison.Ordinal))
            .Select(property => (
                Name: property.Key[Constants.DynamicClientManagementProperties.InternalPropertyPrefix.Length..],
                Value: property.Value))
            .Where(property => !ExplicitlyMappedProperties.Contains(property.Name));
        
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
        return client.Properties.TryGetValue(
            Constants.DynamicClientManagementProperties.InternalPropertyPrefix + propertyName,
            out var value)
            ? value
            : null;
    }

    private static string[]? GetContacts(Client client)
    {
        var value = GetProperty(client, Constants.ClientMetadata.Contacts);
        if (value is null)
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
        var value = GetProperty(client, propertyName);
        if (value is null)
        {
            return null;
        }

        return ParseJsonProperty(propertyName, value);
    }
}
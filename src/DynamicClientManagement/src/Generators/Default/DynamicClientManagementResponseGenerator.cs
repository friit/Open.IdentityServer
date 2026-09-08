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
        "software_id",
        "software_version"
    };

    public Task<RegisterResponseDTO> GenerateAsync(DynamicClientManagementRegisterResult registrationRegisterResult, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
    
        var response = new RegisterResponseDTO
        {
            ClientName = registrationRegisterResult.Client.ClientName,
            ClientId = registrationRegisterResult.Client.ClientId,
            ClientSecret = registrationRegisterResult.ClientSecret,
            ClientIdIssuedAt = registrationRegisterResult.ClientIdIssuedAt.ToUnixTimeSeconds(),
            ClientSecretExpiresAt = registrationRegisterResult.ClientSecretExpiresAt?.ToUnixTimeSeconds(),
            RegistrationAccessToken = registrationRegisterResult.RegistrationAccessToken,
            RegistrationClientUri = registrationRegisterResult.RegistrationClientUri,
            RedirectUris = registrationRegisterResult.Client.RedirectUris?.ToArray(),
            GrantTypes = registrationRegisterResult.Client.AllowedGrantTypes?.ToArray(),
            //TODO: Better way to presist this? techincally the default here is fragile
            TokenEndpointAuthMethod = registrationRegisterResult.OriginalRequestDto.TokenEndpointAuthMethod,
            ResponseTypes = registrationRegisterResult.OriginalRequestDto.ResponseTypes?.ToArray(),
            ClientUri = registrationRegisterResult.Client.ClientUri,
            LogoUri = registrationRegisterResult.Client.LogoUri,
            Contacts = GetContacts(registrationRegisterResult.Client),
            TermsOfServiceUri = GetProperty(registrationRegisterResult.Client, "tos_uri"),
            PolicyUri = GetProperty(registrationRegisterResult.Client, "policy_uri"),
            JsonWebKeySetUri = GetProperty(registrationRegisterResult.Client, "jwks_uri"),
            JsonWebKeySet = GetJsonProperty(registrationRegisterResult.Client, "jwks"),
            SoftwareId = GetProperty(registrationRegisterResult.Client, "software_id"),
            SoftwareVersion = GetProperty(registrationRegisterResult.Client, "software_version"),
            
        };

        if (registrationRegisterResult.Client.AllowedScopes != null && registrationRegisterResult.Client.AllowedScopes.Any())
        {
            response.Scope = string.Join(" ", registrationRegisterResult.Client.AllowedScopes);
        }
    
        if (registrationRegisterResult.OriginalRequestDto.Enabled.HasValue)
        {
            response.AdditionalData["enabled"] = JsonSerializer.SerializeToElement(registrationRegisterResult.Client.Enabled);
        }
        
        

        var additionalData = registrationRegisterResult.Client.Properties
            .Where(property => property.Key.StartsWith(InternalPropertyPrefix, StringComparison.Ordinal))
            .Select(property => new
            {
                Name = property.Key[InternalPropertyPrefix.Length..],
                property.Value
            })
            .Where(property => !IgnoredProperties.Contains(property.Name));

        foreach (var property in additionalData)
        {
            response.AdditionalData[property.Name] = ParseJsonProperty(property.Name, property.Value);
        }
    

        return Task.FromResult(response);
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
using System.Text.Json;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;
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

    public Task<RegisterResponse> GenerateAsync(DynamicClientManagementResult registrationResult, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
    
        var response = new RegisterResponse
        {
            ClientName = registrationResult.Client.ClientName,
            ClientId = registrationResult.Client.ClientId,
            ClientSecret = registrationResult.ClientSecret,
            ClientIdIssuedAt = registrationResult.ClientIdIssuedAt.ToUnixTimeSeconds(),
            ClientSecretExpiresAt = registrationResult.ClientSecretExpiresAt?.ToUnixTimeSeconds(),
            RegistrationAccessToken = registrationResult.RegistrationAccessToken,
            RegistrationClientUri = registrationResult.RegistrationClientUri,
            RedirectUris = registrationResult.Client.RedirectUris?.ToArray(),
            GrantTypes = registrationResult.Client.AllowedGrantTypes?.ToArray(),
            //TODO: Better way to presist this? techincally the default here is fragile
            TokenEndpointAuthMethod = registrationResult.OriginalRequest.TokenEndpointAuthMethod,
            ResponseTypes = registrationResult.OriginalRequest.ResponseTypes?.ToArray(),
            ClientUri = registrationResult.Client.ClientUri,
            LogoUri = registrationResult.Client.LogoUri,
            Contacts = GetContacts(registrationResult.Client),
            TermsOfServiceUri = GetProperty(registrationResult.Client, "tos_uri"),
            PolicyUri = GetProperty(registrationResult.Client, "policy_uri"),
            JsonWebKeySetUri = GetProperty(registrationResult.Client, "jwks_uri"),
            JsonWebKeySet = GetJsonProperty(registrationResult.Client, "jwks"),
            SoftwareId = GetProperty(registrationResult.Client, "software_id"),
            SoftwareVersion = GetProperty(registrationResult.Client, "software_version"),
            
        };

        if (registrationResult.Client.AllowedScopes != null && registrationResult.Client.AllowedScopes.Any())
        {
            response.Scope = string.Join(" ", registrationResult.Client.AllowedScopes);
        }
    
        if (registrationResult.OriginalRequest.Enabled.HasValue)
        {
            response.AdditionalData["enabled"] = JsonSerializer.SerializeToElement(registrationResult.Client.Enabled);
        }
        
        

        var additionalData = registrationResult.Client.Properties
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
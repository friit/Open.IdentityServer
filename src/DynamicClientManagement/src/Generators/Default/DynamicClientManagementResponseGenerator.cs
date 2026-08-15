using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;

namespace Open.IdentityServer.DynamicClientManagement.Generators.Default;

public class DynamicClientManagementResponseGenerator : IDynamicClientManagementResponseGenerator
{
    public Task<RegisterResponse> GenerateAsync(DynamicClientManagementResult registrationResult, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var response = new RegisterResponse
        {
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
        };

        if (registrationResult.Client.AllowedScopes != null && registrationResult.Client.AllowedScopes.Any())
        {
            response.Scope = string.Join(" ", registrationResult.Client.AllowedScopes);
        }
        
        
        if (registrationResult.OriginalRequest.Enabled.HasValue)
        {
            response.AdditionalData["enabled"] = JsonSerializer.SerializeToElement(registrationResult.Client.Enabled);
        }
        
        if (registrationResult.OriginalRequest.ClientName != null)
        {
            response.AdditionalData["client_name"] = JsonSerializer.SerializeToElement(registrationResult.Client.ClientName);
        }
        
        if (registrationResult.OriginalRequest.ClientUri != null)
        {
            response.AdditionalData["client_uri"] = JsonSerializer.SerializeToElement(registrationResult.Client.ClientUri);
        }
        
        if (registrationResult.Client.LogoUri != null)
        {
            response.AdditionalData["logo_uri"] = JsonSerializer.SerializeToElement(registrationResult.Client.LogoUri);
        }
        
        if (registrationResult.Client.Properties.TryGetValue("contacts", out var contacts))
        {
            response.AdditionalData["contacts"] = JsonSerializer.SerializeToElement(contacts);
        }
        
        if (registrationResult.Client.Properties.TryGetValue("policy_uri", out var policyUri))
        {
            response.AdditionalData["policy_uri"] = JsonSerializer.SerializeToElement(policyUri);
        }
        
        if (registrationResult.Client.Properties.TryGetValue("tos_uri", out var tosUri))    
        {
            response.AdditionalData["tos_uri"] = JsonSerializer.SerializeToElement(tosUri);
        }
        
        if (registrationResult.Client.Properties.TryGetValue("software_id", out var softwareId))
        {
            response.AdditionalData["software_id"] = JsonSerializer.SerializeToElement(softwareId);
        }
        
        if (registrationResult.Client.Properties.TryGetValue("software_version", out var softwareVersion))
        {
            response.AdditionalData["software_version"] = JsonSerializer.SerializeToElement(softwareVersion);
        }
        

        return Task.FromResult(response);
    }
}
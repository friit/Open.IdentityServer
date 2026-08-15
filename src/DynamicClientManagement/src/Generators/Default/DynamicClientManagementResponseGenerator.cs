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

        return Task.FromResult(response);
    }
}
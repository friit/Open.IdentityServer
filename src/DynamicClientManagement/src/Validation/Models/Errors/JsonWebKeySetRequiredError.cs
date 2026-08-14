namespace Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;

public record JsonWebKeySetRequiredError() 
    : ClientMetadataValidationError("jwks must be provided when token endpoint auth method is private_key_jwt")
{
    
}
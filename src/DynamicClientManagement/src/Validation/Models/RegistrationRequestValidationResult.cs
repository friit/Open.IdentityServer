using Open.IdentityServer.DynamicClientManagement.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Models;

public class RegistrationRequestValidationResult
{
    public bool IsError { get; set; }
    public string? ErrorDescription { get; set; }
    public string Error { get; set; }
    
    public RegisterRequest? Request { get; private set; }

    public RegistrationRequestValidationResult(RegisterRequest request)
    {
        Request = request;
        IsError = false;
    }
    
    public RegistrationRequestValidationResult(RegisterRequest request, string error, string? errorDescription = null) : base()
    {
        Request = request;
        Error = error;
        ErrorDescription = errorDescription;
        IsError = true;
    }
}
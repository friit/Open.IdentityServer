using Open.IdentityServer.Extensions;

namespace Open.IdentityServer.DynamicClientRegistration.Models.Results;

public class DynamicClientRegistrationValidationResult
{
    public bool IsValid() => Error.IsNullOrEmpty(); 
    public string? Error { get; private set; }
    public string? ErrorDescription { get; private set; }
    public RegisterRequest? Request { get; private set; }

    private DynamicClientRegistrationValidationResult(){}
    
    public static DynamicClientRegistrationValidationResult CreateSuccess(RegisterRequest request)
    {
        return new DynamicClientRegistrationValidationResult
        {
            Request = request
        };
    }
    
    public static DynamicClientRegistrationValidationResult CreateFailure(string error, string errorDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        
        return new DynamicClientRegistrationValidationResult
        {
            Error = error,
            ErrorDescription = errorDescription
        };
    }
}
namespace Open.IdentityServer.DynamicClientRegistration;

internal static class Constants
{
    internal static class RegisterErrors
    {
        internal const string InvalidClientMetadata = "invalid_client_metadata";
        internal const string InvalidRedirectUri = "invalid_redirect_uri";
        internal const string InvalidRequest = "invalid_request";
        internal const string InvalidSoftwareStatement = "invalid_software_statement";
        internal const string ServerError = "server_error";
        internal const string UnapprovedSoftwareStatement = "unapproved_software_statement";
    }
}

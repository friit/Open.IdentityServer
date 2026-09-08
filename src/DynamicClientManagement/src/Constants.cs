// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Open.IdentityServer.DynamicClientManagement;

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
        internal const string InvalidClient = "invalid_client";
        public const string MissingBearerToken = "missing_bearer_token";
        public const string InvalidBearerToken = "invalid_bearer_token";
    }

    internal static class TokenEndpointAuthMethod
    {
        internal const string PrivateKeyJwt = "private_key_jwt";
        internal const string ClientSecretPost = "client_secret_post";
        internal const string ClientSecretBasic = "client_secret_basic";
    }
}

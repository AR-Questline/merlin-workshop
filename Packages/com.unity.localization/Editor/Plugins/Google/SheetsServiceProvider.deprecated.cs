using System;

namespace UnityEditor.Localization.Plugins.Google
{
    public partial class SheetsServiceProvider
    {
        /// <inheritdoc cref="SetOAuthCredentials"/>
        [Obsolete("SetOAuthCrendtials is deprecated, use SetOAuthCredentials instead. (UnityUpgrade) -> SetOAuthCredentials(*)")]
        public void SetOAuthCrendtials(string credentialsJson) => SetOAuthCredentials(credentialsJson);

        /// <inheritdoc cref="SetOAuthCredentials"/>
        [Obsolete("SetOAuthCrendtials is deprecated, use SetOAuthCredentials instead. (UnityUpgrade) -> SetOAuthCredentials(*)")]
        public void SetOAuthCrendtials(string clientId, string clientSecret) => SetOAuthCredentials(clientId, clientSecret);

        /// <inheritdoc cref="AuthorizeOAuth"/>
        [Obsolete("AuthoizeOAuth is deprecated, use AuthorizeOAuth instead. (UnityUpgrade) -> AuthorizeOAuth(*)")]
        public void AuthoizeOAuth() => AuthorizeOAuth();
    }
}

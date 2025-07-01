using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using UnityEngine;

namespace UnityEditor.Localization.Plugins.Google
{
    /// <summary>
    /// See https://cloud.google.com/docs/authentication
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// No authentication has been specified.
        /// </summary>
        None,

        /// <summary>
        /// Accessing private data.
        /// </summary>
        OAuth,

        /// <summary>
        /// Accessing public data anonymously.
        /// </summary>
        APIKey
    }

    /// <summary>
    /// Configuration for connecting to a Google Sheet.
    /// </summary>
    public interface IGoogleSheetsService
    {
        /// <summary>
        /// The Google Sheet service that will be created using the Authorization API.
        /// </summary>
        SheetsService Service { get; }
    }

    /// <summary>
    /// The Sheets service provider performs the authentication to Google and keeps track of the authentication tokens
    /// so that you do not need to authenticate each time.
    /// The Sheets service provider also includes general sheet properties, such as default sheet styles, that are used when creating a new sheet.
    /// </summary>
    /// <example>
    /// Unity recommends to have a <see cref="SheetsServiceProvider"/> asset pre-configured for use, however this example does create a new one.
    /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="sheets-service-provider"/>
    /// </example>
    [CreateAssetMenu(fileName = "Google Sheets Service", menuName = "Localization/Google Sheets Service")]
    [HelpURL("https://developers.google.com/sheets/api/guides/authorizing#AboutAuthorization")]
    public partial class SheetsServiceProvider : ScriptableObject, IGoogleSheetsService, ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_ApiKey;

        [SerializeField]
        string m_ClientId;

        [SerializeField]
        string m_ClientSecret;

        [SerializeField]
        AuthenticationType m_AuthenticationType;

        [SerializeField]
        string m_ApplicationName;

        [SerializeField]
        NewSheetProperties m_NewSheetProperties = new NewSheetProperties();

        SheetsService m_SheetsService;

        // The Google API access application we are requesting.
        static readonly string[] k_Scopes = { SheetsService.Scope.Spreadsheets };

        /// <summary>
        /// Used to make sure the access and refresh tokens persist. Uses a FileDataStore by default with "Library/Google/{name}" as the path.
        /// </summary>
        public IDataStore DataStore { get; set; }

        /// <summary>
        /// The Google Sheet service that will be created using the Authorization API.
        /// </summary>
        public virtual SheetsService Service
        {
            get
            {
                if (m_SheetsService == null)
                    m_SheetsService = Connect();
                return m_SheetsService;
            }
        }

        /// <summary>
        /// The authorization methodology to use.
        /// See <see href="https://developers.google.com/sheets/api/guides/authorizing"/>
        /// </summary>
        public AuthenticationType Authentication => m_AuthenticationType;

        /// <summary>
        /// The API Key to use when using <see cref="AuthenticationType.APIKey"/> authentication.
        /// </summary>
        public string ApiKey => m_ApiKey;

        /// <summary>
        /// <para>Client Id when using OAuth authentication.</para>
        /// See also <seealso cref="SetOAuthCredentials"/>
        /// </summary>
        public string ClientId => m_ClientId;

        /// <summary>
        /// <para>Client secret when using OAuth authentication.</para>
        /// See also <seealso cref="SetOAuthCredentials"/>
        /// </summary>
        public string ClientSecret => m_ClientSecret;

        /// <summary>
        /// The name of the application that will be sent when connecting.
        /// </summary>
        public string ApplicationName
        {
            get => m_ApplicationName;
            set => m_ApplicationName = value;
        }

        /// <summary>
        /// Properties to use when creating a new Google Spreadsheet sheet.
        /// </summary>
        public NewSheetProperties NewSheetProperties
        {
            get => m_NewSheetProperties;
            set => m_NewSheetProperties = value;
        }

        /// <summary>
        /// Set the API Key. An API key can only be used for reading from a public Google Spreadsheet.
        /// </summary>
        /// <param name="apiKey"></param>
        public void SetApiKey(string apiKey)
        {
            m_ApiKey = apiKey;
            m_AuthenticationType = AuthenticationType.APIKey;
        }

        /// <summary>
        /// Enable OAuth 2.0 authentication and extract the <see cref="ClientId"/> and <see cref="ClientSecret"/> from the supplied json.
        /// </summary>
        /// <param name="credentialsJson"></param>
        public void SetOAuthCredentials(string credentialsJson)
        {
            var secrets = LoadSecrets(credentialsJson);
            m_ClientId = secrets.ClientId;
            m_ClientSecret = secrets.ClientSecret;
            m_AuthenticationType = AuthenticationType.OAuth;
        }

        /// <summary>
        /// Enable OAuth 2.0 authentication with the provided client Id and client secret.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        public void SetOAuthCredentials(string clientId, string clientSecret)
        {
            m_ClientId = clientId;
            m_ClientSecret = clientSecret;
            m_AuthenticationType = AuthenticationType.OAuth;
        }

        SheetsService Connect()
        {
            if (Authentication == AuthenticationType.None)
                throw new Exception("No connection credentials. You must provide either OAuth2.0 credentials or an Api Key.");

            if (Authentication == AuthenticationType.OAuth)
                return ConnectWithOAuth2();
            return ConnectWithApiKey();
        }

        /// <summary>
        /// When calling an API that does not access private user data, you can use a simple API key.
        /// This key is by Google to authenticate your application for accounting purposes.
        /// If you do need to access private user data, you must use OAuth 2.0.
        /// </summary>
        SheetsService ConnectWithApiKey()
        {
            SheetsService sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                ApiKey = m_ApiKey,
                ApplicationName = ApplicationName
            });
            return sheetsService;
        }

        /// <summary>
        /// Call to preauthorize when using OAuth authorization. This will cause a browser to open a Google authorization
        /// page after which the token will be stored in IDataStore so that this does not need to be done each time.
        /// If this is not called then the first time <see cref="Service"/> is called it will be performed then.
        /// </summary>
        /// <returns></returns>
        public UserCredential AuthorizeOAuth()
        {
            // Prevents Unity locking up if the user canceled the auth request.
            // Auto cancel after 60 secs
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            var connectTask = AuthorizeOAuthAsync(cts.Token);
            if (!connectTask.IsCompleted)
                connectTask.RunSynchronously();

            if (connectTask.Status == TaskStatus.Faulted)
            {
                throw new Exception($"Failed to connect to Google Sheets.\n{connectTask.Exception}");
            }
            return connectTask.Result;
        }

        /// <summary>
        /// Call to preauthorize when using OAuth authorization. This will cause a browser to open a Google authorization
        /// page after which the token will be stored in IDataStore so that this does not need to be done each time.
        /// If this is not called then the first time <see cref="Service"/> is called it will be performed then.
        /// </summary>
        /// <param name="cancellationToken">Token that can be used to cancel the task prematurely.</param>
        /// <returns>The authorization Task that can be monitored.</returns>
        public Task<UserCredential> AuthorizeOAuthAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ClientSecret))
                throw new Exception($"{nameof(ClientSecret)} is empty");

            if (string.IsNullOrEmpty(ClientId))
                throw new Exception($"{nameof(ClientId)} is empty");

            // We create a separate area for each so that multiple providers don't clash.
            var dataStore = DataStore ?? new FileDataStore($"Library/Google/{name}", true);

            var secrets = new ClientSecrets { ClientId = m_ClientId, ClientSecret = m_ClientSecret };

            // We use the client Id for the user so that we can generate a unique token file and prevent conflicts when using multiple OAuth authentications. (LOC-188)
            var user = m_ClientId;
            var connectTask = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, k_Scopes, user, cancellationToken, dataStore);
            return connectTask;
        }

        /// <summary>
        /// When calling an API that will access private user data, O Auth 2.0 credentials must be used.
        /// </summary>
        SheetsService ConnectWithOAuth2()
        {
            var userCredentials = AuthorizeOAuth();
            var sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = userCredentials,
                ApplicationName = ApplicationName,
            });
            return sheetsService;
        }

        internal static ClientSecrets LoadSecrets(string credentials)
        {
            if (string.IsNullOrEmpty(credentials))
                throw new ArgumentException(nameof(credentials));

            using (var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(credentials)))
            {
                var gcs = GoogleClientSecrets.FromStream(stream);
                return gcs.Secrets;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(m_ApplicationName))
                m_ApplicationName = PlayerSettings.productName;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
    }
}

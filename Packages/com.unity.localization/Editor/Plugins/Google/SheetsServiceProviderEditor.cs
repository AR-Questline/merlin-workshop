using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using UnityEngine;

namespace UnityEditor.Localization.Plugins.Google
{
    [CustomEditor(typeof(SheetsServiceProvider))]
    class SheetsServiceProviderEditor : UnityEditor.Editor
    {
        class Styles
        {
            public static readonly GUIContent apiKey = EditorGUIUtility.TrTextContent("API Key");
            public static readonly GUIContent authorize = EditorGUIUtility.TrTextContent("Authorize...", "Authorize the user. This is not required however the first time a connection to a Google sheet is required then authorization will be required.");
            public static readonly GUIContent authentication = EditorGUIUtility.TrTextContent("Authentication");
            public static readonly GUIContent cancel = EditorGUIUtility.TrTextContent("Cancel Authentication");
            public static readonly GUIContent clientId = EditorGUIUtility.TrTextContent("Client Id");
            public static readonly GUIContent clientSecret = EditorGUIUtility.TrTextContent("Client Secret");
            public static readonly GUIContent noCredentials = EditorGUIUtility.TrTextContent("No Credentials Selected");
            public static readonly GUIContent loadCredentials = EditorGUIUtility.TrTextContent("Load Credentials...", "Load the credentials from a json file");
        }

        SerializedProperty m_ClientId;
        SerializedProperty m_ClientSecret;
        SerializedProperty m_ApiKey;
        SerializedProperty m_AuthenticationType;
        SerializedProperty m_ApplicationName;
        SerializedProperty m_NewSheetProperties;

        static Task<UserCredential> s_AuthorizeTask;
        static CancellationTokenSource s_CancellationToken;

        public void OnEnable()
        {
            m_ClientId = serializedObject.FindProperty("m_ClientId");
            m_ClientSecret = serializedObject.FindProperty("m_ClientSecret");
            m_ApiKey = serializedObject.FindProperty("m_ApiKey");
            m_AuthenticationType = serializedObject.FindProperty("m_AuthenticationType");
            m_ApplicationName = serializedObject.FindProperty("m_ApplicationName");
            m_NewSheetProperties = serializedObject.FindProperty("m_NewSheetProperties");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_ApplicationName);
            EditorGUILayout.PropertyField(m_AuthenticationType, Styles.authentication);

            var auth = (AuthenticationType)m_AuthenticationType.intValue;

            if (auth == AuthenticationType.APIKey)
            {
                EditorGUILayout.HelpBox("API Key can be used for reading from public sheets only.", MessageType.Info);

                EditorGUILayout.PropertyField(m_ApiKey, Styles.apiKey);
            }
            else if (auth == AuthenticationType.OAuth)
            {
                EditorGUILayout.HelpBox("OAuth 2.0 authorization allows reading and writing to both public and private sheets.", MessageType.Info);

                EditorGUILayout.PropertyField(m_ClientId);
                EditorGUILayout.PropertyField(m_ClientSecret);

                if (GUILayout.Button(Styles.loadCredentials))
                {
                    var file = EditorUtility.OpenFilePanel(Styles.loadCredentials.text, "", "json");
                    if (!string.IsNullOrEmpty(file))
                    {
                        var json = File.ReadAllText(file);
                        var secrets = SheetsServiceProvider.LoadSecrets(json);
                        m_ClientId.stringValue = secrets.ClientId;
                        m_ClientSecret.stringValue = secrets.ClientSecret;
                    }
                }

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(m_ClientId.stringValue) || string.IsNullOrEmpty(m_ClientSecret.stringValue));

                if (s_AuthorizeTask != null)
                {
                    if (GUILayout.Button(Styles.cancel))
                    {
                        s_CancellationToken.Cancel();
                    }

                    if (s_AuthorizeTask.IsCompleted)
                    {
                        if (s_AuthorizeTask.Status == TaskStatus.RanToCompletion)
                            Debug.Log($"Authorized: {s_AuthorizeTask.Result.Token.IssuedUtc}", target);
                        else if (s_AuthorizeTask.Exception != null)
                            Debug.LogException(s_AuthorizeTask.Exception, target);
                        s_AuthorizeTask = null;
                        s_CancellationToken = null;
                    }
                }
                else
                {
                    if (GUILayout.Button(Styles.authorize))
                    {
                        var provider = target as SheetsServiceProvider;
                        s_CancellationToken = new CancellationTokenSource();
                        s_AuthorizeTask = provider.AuthorizeOAuthAsync(s_CancellationToken.Token);
                    }
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(m_NewSheetProperties, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

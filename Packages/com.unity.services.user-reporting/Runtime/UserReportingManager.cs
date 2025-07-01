using System;
using Unity.Services.UserReporting.Client;
using UnityEngine;

namespace Unity.Services.UserReporting
{
    static class UserReportingManager
    {
        static UserReportingClient s_CurrentClient;

        internal static UserReportingClient CurrentClient
        {
            get
            {
                if (s_CurrentClient == null)
                {
                    Configure();
                }

                return s_CurrentClient;
            }
            private set => s_CurrentClient = value;
        }

        internal static void Configure(string endpoint, string projectIdentifier,
            UserReportingClientConfiguration configuration)
        {
            CurrentClient = new UserReportingClient(endpoint, projectIdentifier, configuration);
            if (string.IsNullOrEmpty(CurrentClient.ProjectIdentifier))
            {
                WarnConfigurationFailure();
            }
        }

        internal static void WarnConfigurationFailure()
        {
            Debug.LogWarning(
                "The Unity Project ID is not set. Open the Services window (Window > General > Services) to create or select an existing Unity Project ID to use User Reporting.");
        }

        public static void Configure(string projectIdentifier, UserReportingClientConfiguration configuration)
        {
            Configure(s_Endpoint, projectIdentifier, configuration);
        }

        public static void Configure(string projectIdentifier)
        {
            Configure(s_Endpoint, projectIdentifier, new UserReportingClientConfiguration());
        }

        public static void Configure()
        {
            Configure(s_Endpoint, Application.cloudProjectId, new UserReportingClientConfiguration());
        }

        public static void Configure(UserReportingClientConfiguration configuration)
        {
            Configure(s_Endpoint, Application.cloudProjectId, configuration);
        }

        public static void SampleMetric(string name, double value)
        {
            CurrentClient.SampleMetric(name, value);
        }

        public static void TakeScreenshot(int maximumWidth, int maximumHeight, object source = null)
        {
            CurrentClient.TakeScreenshot(maximumWidth, maximumHeight, source);
        }

        internal static void Use(UserReportingClient client)
        {
            if (client != null)
            {
                CurrentClient = client;
            }
        }

        public static bool HasProjectConfiguration()
        {
            return !string.IsNullOrEmpty(CurrentClient.ProjectIdentifier);
        }

        static string s_Endpoint = "https://userreporting.cloud.unity3d.com";

        public static void LogEvent(UserReportEventLevel level, string message)
        {
            CurrentClient.Events.Add(new UserReportEvent(
                level,
                message,
                CurrentClient.FrameNumber,
                null,
                DateTime.UtcNow));
        }
    }
}

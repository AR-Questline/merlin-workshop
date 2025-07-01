using System;
using System.Linq;
using Unity.Services.Core.Scheduler.Internal;
using Unity.Services.UserReporting.Client;
using UnityEngine;

namespace Unity.Services.UserReporting.Internal
{
    class UserReportingServiceInternal : IUserReporting
    {
        internal UserReportingServiceInternal(string version, string installationIdentifier,
                                              IActionScheduler scheduler)
        {
            Version = version;
            InstallationIdentifier = installationIdentifier;
            Scheduler = scheduler;
            // Creates a DontDestroyOnLoad GameObject for main Unity thread purposes.
            UserReportingSceneHelper.Initialize();
        }

        internal string Version { get; }

        internal string InstallationIdentifier { get; }

        internal IActionScheduler Scheduler { get; }


        UserReport m_Report;

        public bool SendInternalMetrics
        {
            get => UserReportingManager.CurrentClient.SendInternalMetrics;
            set => UserReportingManager.CurrentClient.SendInternalMetrics = value;
        }

        [Obsolete("SendEventsToAnalytics is deprecated, please use the UGS Analytics SDK for your needs.")]
        public bool SendEventsToAnalytics { get; set; }

        public void Configure(UserReportingClientConfiguration configuration = null, string projectIdentifier = null)
        {
            if (configuration is null)
            {
                configuration = new UserReportingClientConfiguration();
            }

            if (projectIdentifier is not null)
            {
                UserReportingManager.Configure(projectIdentifier, configuration);
            }
            else
            {
                UserReportingManager.Configure(configuration);
            }
        }

        public void SetProjectIdentifier(string projectIdentifier = null)
        {
            UserReportingManager.CurrentClient.ProjectIdentifier = projectIdentifier;
        }

        public bool HasOngoingReport => m_Report is not null;

        public void TakeScreenshot(int maximumWidth, int maximumHeight, object source = null)
        {
            UserReportingManager.TakeScreenshot(maximumWidth, maximumHeight, source);
        }

        public void CreateNewUserReport(Action callback = null)
        {
            UserReportingManager.CurrentClient.CreateUserReport(report =>
            {
                m_Report = report;
            });
            callback?.Invoke();
        }

        public void ClearOngoingReport()
        {
            m_Report = null;
        }

        public void AddAttachmentToReport(string title, string filename, byte[] data, string mediaType = "")
        {
            UserReportingManager.CurrentClient.Attachments.Add(new UserReportAttachment(title, filename,
                mediaType, data));
        }

        public Texture2D GetLatestScreenshot()
        {
            if (UserReportingManager.CurrentClient.Screenshots.Count > 0)
            {
                var screenshot = UserReportingManager.CurrentClient.Screenshots.ToList().Last();
                byte[] data = Convert.FromBase64String(screenshot.DataBase64);
                Texture2D texture = new Texture2D(1, 1);
                texture.LoadImage(data);
                return texture;
            }

            return null;
        }

        public void SetReportSummary(string summaryInputText)
        {
            if (m_Report is null)
            {
                CreateNewUserReport();
            }
            m_Report.Summary = summaryInputText;
        }

        public void AddMetadata(string name, string value)
        {
            UserReportingManager.CurrentClient.AddDeviceMetadata(name, value);
        }

        public void AddDimensionValue(string dimension, string value)
        {
            m_Report.Dimensions.Add(new UserReportNamedValue(dimension, value));
        }

        public void SetReportDescription(string description)
        {
            if (m_Report is null)
            {
                CreateNewUserReport();
            }
            m_Report.Fields.Add(new UserReportNamedValue("Description", description));
        }

        public void SendUserReport(Action<float> progressUpdate, Action<bool> result)
        {
            UserReportingManager.CurrentClient.SendUserReport(m_Report,
                progressUpdate, null, result, null);
        }

        public void SampleMetric(string name, double value)
        {
            UserReportingManager.CurrentClient.SampleMetric(name, value);
        }

        public void SetEndpoint(string endpoint)
        {
            UserReportingManager.CurrentClient.Endpoint = endpoint;
        }

        public void LogEvent(UserReportEventLevel level, string message)
        {
            UserReportingManager.LogEvent(level, message);
        }
    }
}

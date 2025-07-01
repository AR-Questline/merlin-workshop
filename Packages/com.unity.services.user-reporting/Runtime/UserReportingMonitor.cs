using System;
using Unity.Services.UserReporting.Client;
using UnityEngine;

namespace Unity.Services.UserReporting
{
    class UserReportingMonitor : MonoBehaviour
    {
        internal UserReportingMonitor()
        {
            isEnabled = true;
            isHiddenWithoutDimension = true;
            Type type = GetType();
            monitorName = type.Name;
        }

        [Tooltip("Value indicating whether the monitor is enabled.")]
        #pragma warning disable CS0414
        [SerializeField] bool isEnabled;
        #pragma warning restore CS0414

        [Tooltip("Value indicating whether the monitor is enabled after it is triggered.")]
        [SerializeField]
        bool isEnabledAfterTrigger;

        [Tooltip("Value indicating whether the user report has IsHiddenWithoutDimension set.")]
        [SerializeField]
        bool isHiddenWithoutDimension;

        [Tooltip("The monitor name.")]
        [SerializeField]
        string monitorName;

        [Tooltip("A summary of the monitor.")]
        [SerializeField]
        string summary;

        void Start()
        {
            if (UserReportingManager.CurrentClient == null)
            {
                UserReportingManager.Configure();
            }
        }

        protected void Trigger()
        {
            if (!isEnabledAfterTrigger)
            {
                isEnabled = false;
            }

            UserReportingManager.CurrentClient.CreateUserReport(report =>
            {
                report.Summary = summary;
                report.DeviceMetadata.Add(new UserReportNamedValue("Monitor", monitorName));
                string platform = "Unknown";
                string version = "0.0";
                foreach (var deviceMetadata in report.DeviceMetadata)
                {
                    if (deviceMetadata.Name == "Platform")
                    {
                        platform = deviceMetadata.Value;
                    }

                    if (deviceMetadata.Name == "Version")
                    {
                        version = deviceMetadata.Value;
                    }
                }

                report.Dimensions.Add(new UserReportNamedValue("Monitor.Platform.Version",
                    $"{monitorName}.{platform}.{version}"));
                report.Dimensions.Add(new UserReportNamedValue("Monitor", monitorName));
                report.IsHiddenWithoutDimension = isHiddenWithoutDimension;
                UserReportingManager.CurrentClient.SendUserReport(report, null, null);
            });
        }
    }
}

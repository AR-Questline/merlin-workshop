#if UNITY_IOS || UNITY_IPHONE
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Localization.Platform.iOS;
using UnityEngine.Localization.Settings;

namespace UnityEditor.Localization.Platform.iOS
{
    class LocalizationBuildPlayerIOS : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string k_Placeholder = "<CONTROLLED BY LOCALIZATION>";
        public int callbackOrder => 1;

        bool m_ClearCameraUsage;
        bool m_ClearLocationUsage;
        bool m_ClearMicUsage;

        public void OnPreprocessBuild(BuildReport report)
        {
            var appInfo = LocalizationSettings.Metadata.GetMetadata<AppInfo>();
            if (appInfo == null)
            {
                Debug.LogWarning("iOS App Info has not been configured. Please add and configure `iOS App Info` metadata to the Localization Settings in order to correctly support Localization on iOS.");
            }
            else
            {
                bool wasDirty = IsPlayerSettingsDirty();

                if (!appInfo.CameraUsageDescription.IsEmpty && string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
                {
                    PlayerSettings.iOS.cameraUsageDescription = k_Placeholder;
                    m_ClearCameraUsage = true;
                }

                if (!appInfo.LocationUsageDescription.IsEmpty && string.IsNullOrEmpty(PlayerSettings.iOS.locationUsageDescription))
                {
                    PlayerSettings.iOS.locationUsageDescription = k_Placeholder;
                    m_ClearLocationUsage = true;
                }

                if (!appInfo.MicrophoneUsageDescription.IsEmpty && string.IsNullOrEmpty(PlayerSettings.iOS.microphoneUsageDescription))
                {
                    PlayerSettings.iOS.microphoneUsageDescription = k_Placeholder;
                    m_ClearMicUsage = true;
                }

                if (!wasDirty)
                    ClearPlayerSettingsDirtyFlag();
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            bool wasDirty = IsPlayerSettingsDirty();

            Player.AddLocalizationToXcodeProject(report.summary.outputPath);

            if (m_ClearCameraUsage)
                PlayerSettings.iOS.cameraUsageDescription = string.Empty;

            if (m_ClearLocationUsage)
                PlayerSettings.iOS.locationUsageDescription = string.Empty;

            if (m_ClearMicUsage)
                PlayerSettings.iOS.microphoneUsageDescription = string.Empty;

            if (!wasDirty)
                ClearPlayerSettingsDirtyFlag();
        }

        static bool IsPlayerSettingsDirty()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings != null && settings.Length > 0)
                return EditorUtility.IsDirty(settings[0]);
            return false;
        }

        static void ClearPlayerSettingsDirtyFlag()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings != null && settings.Length > 0)
                EditorUtility.ClearDirty(settings[0]);
        }
    }
}
#endif

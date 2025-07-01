using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace UnityEditor.Localization
{
    class LocalizationBuildPlayer : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        LocalizationSettings m_Settings;

        bool m_RemoveFromPreloadedAssets;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            m_RemoveFromPreloadedAssets = false;
            m_Settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (m_Settings == null)
                return;

            // Add the localization settings to the preloaded assets.
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            bool wasDirty = IsPlayerSettingsDirty();

            if (!preloadedAssets.Contains(m_Settings))
            {
                ArrayUtility.Add(ref preloadedAssets, m_Settings);
                PlayerSettings.SetPreloadedAssets(preloadedAssets);

                // If we have to add the settings then we should also remove them.
                m_RemoveFromPreloadedAssets = true;

                // Clear the dirty flag so we dont flush the modified file (case 1254502)
                if (!wasDirty)
                    ClearPlayerSettingsDirtyFlag();
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (m_Settings == null || !m_RemoveFromPreloadedAssets)
                return;

            bool wasDirty = IsPlayerSettingsDirty();

            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            ArrayUtility.Remove(ref preloadedAssets, m_Settings);
            PlayerSettings.SetPreloadedAssets(preloadedAssets);

            m_Settings = null;

            // Clear the dirty flag so we dont flush the modified file (case 1254502)
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

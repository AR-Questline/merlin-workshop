#if UNITY_ANDROID
using UnityEditor.Android;

namespace UnityEditor.Localization.Platform.Android
{
    class LocalizationBuildPlayerAndroid : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 1; } }

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            Player.AddLocalizationToAndroidGradleProject(basePath);
        }
    }
}
#endif

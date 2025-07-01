using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Platform.iOS;
using UnityEngine.Localization.Settings;

public class IosAppInfoExample
{
    [MenuItem("Localization/Configure iOS App Info")]
    public static void ConfigureAppInfo()
    {
        var appInfo = LocalizationSettings.Metadata.GetMetadata<AppInfo>();
        if (appInfo == null)
        {
            appInfo = new AppInfo();
            LocalizationSettings.Metadata.AddMetadata(appInfo);
        }

        appInfo.DisplayName = new LocalizedString("My Table", "My Display Name");
        EditorUtility.SetDirty(LocalizationSettings.Instance);
    }
}

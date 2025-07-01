using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Platform.Android;
using UnityEngine.Localization.Settings;

public class AndroidAppInfoExample
{
    [MenuItem("Localization/Configure Android App Info")]
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

    [MenuItem("Localization/Configure Android Icon Info")]
    public static void ConfigureIconInfo()
    {
        var iconInfo = LocalizationSettings.Metadata.GetMetadata<AdaptiveIconsInfo>();
        if (iconInfo == null)
        {
            iconInfo = new AdaptiveIconsInfo();
            LocalizationSettings.Metadata.AddMetadata(iconInfo);
        }

        iconInfo.AdaptiveHdpi = new AdaptiveIcon
        {
            Background = new LocalizedTexture { TableReference = "My Textures", TableEntryReference = "Hdpi Background" },
            Foreground = new LocalizedTexture { TableReference = "My Textures", TableEntryReference = "Hdpi Foreground" }
        };

        EditorUtility.SetDirty(LocalizationSettings.Instance);
    }
}

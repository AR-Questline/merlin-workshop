#if ENABLE_PROPERTY_VARIANTS

using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedObjects;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
#if PACKAGE_UGUI
using UnityEngine.UI;
#endif

#if PACKAGE_TMP
using TMPro;
#endif

#if PACKAGE_UGUI
#region setup-text

public class SetupTextAndFont : MonoBehaviour
{
    public Text text;

    void Start()
    {
        var localizer = gameObject.AddComponent<GameObjectLocalizer>();

        // Gets the Tracked text or creates a new tracker
        var trackedText = localizer.GetTrackedObject<TrackedUGuiGraphic>(text);

        // Gets the Property Variant for the text or creates a new one
        var textVariant = trackedText.GetTrackedProperty<LocalizedStringProperty>("m_Text");

        // The LocalizedString can be modified directly
        textVariant.LocalizedString.SetReference("My String Table", "My Entry");
        textVariant.LocalizedString.Arguments = new object[] {"Argument 1", "Argument 2"};

        // Set up the Font
        var fontVariant = trackedText.GetTrackedProperty<LocalizedAssetProperty>("m_FontData.m_Font");
        fontVariant.LocalizedObject = new LocalizedFont { TableReference = "My Assets", TableEntryReference = "My Font" };

        // Set up a default Font Size and an override size for French and Japanese. All other Locales will use the default Size.
        var fontSize = trackedText.GetTrackedProperty<IntTrackedProperty>("m_FontData.m_FontSize");
        fontSize.SetValue(LocalizationSettings.ProjectLocale.Identifier, 10); // Default Font Size
        fontSize.SetValue("ja", 12); // Japanese Font Size
        fontSize.SetValue("fr", 11); // French Font Size

        // Force an Update
        localizer.ApplyLocaleVariant(LocalizationSettings.SelectedLocale);
    }
}
#endregion

#region setup-dropdown

public class SetupDropdown : MonoBehaviour
{
    public Dropdown dropdown;

    void Start()
    {
        var localizer = gameObject.AddComponent<GameObjectLocalizer>();

        // Gets the Tracked text or creates a new tracker
        var trackedDropdown = localizer.GetTrackedObject<TrackedUGuiDropdown>(dropdown);

        // Setup each option
        for (int i = 0; i < dropdown.options.Count; ++i)
        {
            var optionText = trackedDropdown.GetTrackedProperty<LocalizedStringProperty>($"m_Options.m_Options.Array.data[{i}].m_Text");
            optionText.LocalizedString.SetReference("My String Table", "My Option " + i);
        }

        // Force an Update
        localizer.ApplyLocaleVariant(LocalizationSettings.SelectedLocale);
    }
}
#endregion
#endif

#if PACKAGE_TMP
#region setup-tmp-text

public class SetupTmpTextAndFont : MonoBehaviour
{
    public TextMeshProUGUI text;

    void Start()
    {
        var localizer = gameObject.AddComponent<GameObjectLocalizer>();

        // Gets the Tracked text or creates a new tracker
        var trackedText = localizer.GetTrackedObject<TrackedUGuiGraphic>(text);

        // Gets the Property Variant for the text or creates a new one
        var textVariant = trackedText.GetTrackedProperty<LocalizedStringProperty>("m_text");

        // The LocalizedString can be modified directly
        textVariant.LocalizedString.SetReference("My String Table", "My Entry");
        textVariant.LocalizedString.Arguments = new object[] { "Argument 1", "Argument 2" };

        // Set up the Font
        var fontVariant = trackedText.GetTrackedProperty<LocalizedAssetProperty>("m_FontData.m_Font");
        fontVariant.LocalizedObject = new LocalizedAsset<TMP_FontAsset>() { TableReference = "My Assets", TableEntryReference = "My Font" };

        // Set up a default Font Size and an override size for French and Japanese. All other Locales will use the default Size.
        var fontSize = trackedText.GetTrackedProperty<IntTrackedProperty>("m_FontData.m_FontSize");
        fontSize.SetValue(LocalizationSettings.ProjectLocale.Identifier, 10); // Default Font Size
        fontSize.SetValue("ja", 12); // Japanese Font Size
        fontSize.SetValue("fr", 11); // French Font Size

        // Force an Update
        localizer.ApplyLocaleVariant(LocalizationSettings.SelectedLocale);
    }
}
#endregion

#region setup-tmp-dropdown

public class SetupTmpDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    void Start()
    {
        var localizer = gameObject.AddComponent<GameObjectLocalizer>();

        // Gets the Tracked text or creates a new tracker
        var trackedDropdown = localizer.GetTrackedObject<TrackedTmpDropdown>(dropdown);

        // Setup each option
        for (int i = 0; i < dropdown.options.Count; ++i)
        {
            var optionText = trackedDropdown.GetTrackedProperty<LocalizedStringProperty>($"m_Options.m_Options.Array.data[{i}].m_Text");
            optionText.LocalizedString.SetReference("My String Table", "My Option " + i);
        }

        // Force an Update
        localizer.ApplyLocaleVariant(LocalizationSettings.SelectedLocale);
    }
}
#endregion
#endif

#region setup-rect-transform

public class SetupRectTransform : MonoBehaviour
{
    void Start()
    {
        var localizer = gameObject.AddComponent<GameObjectLocalizer>();

        // Gets the Tracked text or creates a new tracker
        var trackedText = localizer.GetTrackedObject<TrackedRectTransform>(transform);

        // Gets the Property Variant for the x, y and width
        var xPos = trackedText.GetTrackedProperty<FloatTrackedProperty>("m_AnchoredPosition.x");
        var yPos = trackedText.GetTrackedProperty<FloatTrackedProperty>("m_AnchoredPosition.y");
        var width = trackedText.GetTrackedProperty<FloatTrackedProperty>("m_SizeDelta.x");

        xPos.SetValue(LocalizationSettings.ProjectLocale.Identifier, 0); // Default is 0
        xPos.SetValue("ja", 5); // Override for Japanese

        yPos.SetValue(LocalizationSettings.ProjectLocale.Identifier, 10); // Default is 10
        yPos.SetValue("fr", 5); // Override for French

        width.SetValue(LocalizationSettings.ProjectLocale.Identifier, 100); // Default is 100
        width.SetValue("ja", 50); // Japanese requires less space
        width.SetValue("fr", 150); // French requires more space

        // Force an Update
        localizer.ApplyLocaleVariant(LocalizationSettings.SelectedLocale);
    }
}
#endregion


#region user-script

public class MyScript : MonoBehaviour
{
    public string myText;
    public Color textColor;

    void OnGUI()
    {
        GUI.color = textColor;
        GUILayout.Label(myText);
    }
}

public static class MyScriptEditor
{
    public static void SetupLocalization(MyScript script)
    {
        var localizer = script.gameObject.AddComponent<GameObjectLocalizer>();

        // Gets the Tracked text or creates a new tracker
        var trackedScript = localizer.GetTrackedObject<TrackedMonoBehaviourObject>(script);

        // Gets the Property Variant for the text or creates a new one
        var textVariant = trackedScript.GetTrackedProperty<LocalizedStringProperty>(nameof(MyScript.myText));
        textVariant.LocalizedString.SetReference("My String Table Collection", "My Text");

        var redVariant = trackedScript.GetTrackedProperty<FloatTrackedProperty>("textColor.r");
        var greenVariant = trackedScript.GetTrackedProperty<FloatTrackedProperty>("textColor.g");
        var blueVariant = trackedScript.GetTrackedProperty<FloatTrackedProperty>("textColor.b");

        // Default to black text
        redVariant.SetValue("en", 0);
        greenVariant.SetValue("en", 0);
        blueVariant.SetValue("en", 0);

        // Use Red for French
        redVariant.SetValue("fr", 1);

        // Use Green for Japanese
        greenVariant.SetValue("fr", 1);

        // Use white for Arabic
        redVariant.SetValue("ar", 1);
        greenVariant.SetValue("ar", 1);
        blueVariant.SetValue("ar", 1);
    }
}

#endregion

#if MODULE_AUDIO
#region custom-audio

[Serializable]
[DisplayName("Audio Source")]
[CustomTrackedObject(typeof(AudioSource), false)]
public class TrackedAudioSource : TrackedObject
{
    public override AsyncOperationHandle ApplyLocale(Locale variantLocale, Locale defaultLocale)
    {
        var audioClipProperty = GetTrackedProperty("m_audioClip");
        if (audioClipProperty == null)
            return default;

        // Check if the Asset is stored in an Asset Table
        if (audioClipProperty is LocalizedAssetProperty localizedAssetProperty &&
            localizedAssetProperty.LocalizedObject is LocalizedAudioClip localizedAudioClip)
        {
            localizedAudioClip.LocaleOverride = variantLocale;
            var loadHandle = localizedAudioClip.LoadAssetAsync();
            if (loadHandle.IsDone)
                AudioClipLoaded(loadHandle);
            else
            {
                loadHandle.Completed += AudioClipLoaded;
                return loadHandle;
            }
        }
        // Check if the Asset is stored locally
        else if (audioClipProperty is UnityObjectProperty localAssetProperty)
        {
            if (localAssetProperty.GetValue(variantLocale.Identifier, defaultLocale.Identifier, out var clip))
                SetAudioClip(clip as AudioClip);
        }

        return default;
    }

    void AudioClipLoaded(AsyncOperationHandle<AudioClip> loadHandle)
    {
        SetAudioClip(loadHandle.Result);
    }

    void SetAudioClip(AudioClip clip)
    {
        var source = (AudioSource)Target;
        source.Stop();
        source.clip = clip;
        if (clip != null)
            source.Play();
    }

    public override bool CanTrackProperty(string propertyPath)
    {
        // We only care about the Audio clip
        return propertyPath == "m_audioClip";
    }
}

#endregion
#endif

#endif // ENABLE_PROPERTY_VARIANTS

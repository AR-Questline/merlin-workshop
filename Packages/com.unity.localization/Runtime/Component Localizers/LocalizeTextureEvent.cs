using UnityEngine.Localization.Events;

namespace UnityEngine.Localization.Components
{
    /// <summary>
    /// Component that can be used to Localize a [Texture](https://docs.unity3d.com/ScriptReference/Texture.html) asset.
    /// Provides an update event <see cref="LocalizedAssetEvent{TObject, TReference, TEvent}.OnUpdateAsset"/> that can be used to automatically
    /// update the Texture whenever the <see cref="Settings.LocalizationSettings.SelectedLocale"/> or <see cref="LocalizedAssetBehaviour{TObject, TReference}.AssetReference"/> changes.
    /// </summary>
    /// <example>
    /// The example shows how to switch between different Localized Textures.
    /// <code source="../../DocCodeSamples.Tests/LocalizedTextureChanger.cs"/>
    /// </example>
    /// <remarks>
    /// This component can also be added through the **Localize** menu item in the [Raw Image](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-RawImage.html) context menu.
    /// Adding it this way will also automatically configure the Update Asset events to update the [Texture](https://docs.unity3d.com/ScriptReference/Texture.html).
    /// </remarks>
    [AddComponentMenu("Localization/Asset/Localize Texture Event")]
    public class LocalizeTextureEvent : LocalizedAssetEvent<Texture, LocalizedTexture, UnityEventTexture>
    {
    }
}

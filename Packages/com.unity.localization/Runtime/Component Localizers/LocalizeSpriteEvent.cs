using UnityEngine.Localization.Events;

namespace UnityEngine.Localization.Components
{
    /// <summary>
    /// Component that can be used to Localize a [Sprite](https://docs.unity3d.com/ScriptReference/Sprite.html) asset.
    /// Provides an update event <see cref="LocalizedAssetEvent{TObject, TReference, TEvent}.OnUpdateAsset"/> that can be used to automatically
    /// update the Sprite whenever the <see cref="Settings.LocalizationSettings.SelectedLocale"/> or <see cref="LocalizedAssetBehaviour{TObject, TReference}.AssetReference"/> changes.
    /// </summary>
    /// <example>
    /// The example show how it is possible to switch between different Localized Sprites.
    /// ![](../manual/images/scripting/LocalizedSpriteChanger.png)
    /// <code source="../../DocCodeSamples.Tests/LocalizedSpriteChanger.cs"/>
    /// </example>
    /// <remarks>
    /// This component can also be added through the **Localize** menu item in the [Image](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html) context menu.
    /// Adding it this way will also automatically configure the Update Asset events to update the [Image](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html).
    /// </remarks>
    [AddComponentMenu("Localization/Asset/Localize Sprite Event")]
    public class LocalizeSpriteEvent : LocalizedAssetEvent<Sprite, LocalizedSprite, UnityEventSprite>
    {
    }
}

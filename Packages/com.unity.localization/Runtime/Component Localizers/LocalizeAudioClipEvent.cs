#if MODULE_AUDIO || PACKAGE_DOCS_GENERATION

using UnityEngine.Localization.Events;

namespace UnityEngine.Localization.Components
{
    /// <summary>
    /// Component that can be used to Localize an [AudioClip](https://docs.unity3d.com/ScriptReference/AudioClip.html) asset.
    /// Provides an update event <see cref="LocalizedAssetEvent{TObject, TReference, TEvent}.OnUpdateAsset"/> that can be used to automatically
    /// update the clip whenever the <see cref="Settings.LocalizationSettings.SelectedLocale"/> or <see cref="LocalizedAssetBehaviour{TObject, TReference}.AssetReference"/> changes.
    /// </summary>
    /// <example>
    /// This example shows how a Localized Audio Control panel could be created.
    /// The example show how it is possible to switch between different Localized Audio clips.
    /// ![](../manual/images/scripting/LocalizedAudioChanger.png)
    /// <code source="../../DocCodeSamples.Tests/LocalizedAudioChanger.cs"/>
    /// </example>
    /// <remarks>
    /// This component can also be added through the **Localize** menu item in the [Audio Source](https://docs.unity3d.com/Manual/class-AudioSource.html) context menu.
    /// Adding it this way will also automatically configure the Update Asset events to update the [Audio Source](https://docs.unity3d.com/Manual/class-AudioSource.html).
    /// </remarks>
    [AddComponentMenu("Localization/Asset/Localize Audio Clip Event")]
    public class LocalizeAudioClipEvent : LocalizedAssetEvent<AudioClip, LocalizedAudioClip, UnityEventAudioClip>
    {
    }
}

#endif

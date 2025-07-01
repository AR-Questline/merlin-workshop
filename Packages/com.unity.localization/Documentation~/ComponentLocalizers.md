# Component Localizers

Component Localizers are [MonoBehaviours](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) which you can connect to other components/scripts through [UnityEvents](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html). Component Localizers allow for localizing without the need to write any scripts.

## LocalizeStringEvent

The LocalizeStringEvent Component Localizer localizes strings and provides additional arguments when using [Smart Strings](Smart/SmartStrings.md) or [String.Format](https://docs.microsoft.com/dotnet/api/system.string.format). In the Inspector, **Format Arguments** must be [UnityEngine.Objects](https://docs.unity3d.com/ScriptReference/Object.html); however, it is possible to pass any type through as an argument via script. You can also use the LocalizeStringEvent editor to edit tables and Localized strings, and preview them after formatting.

![Localize String Editor.](images/LocalizedString_Editor.gif)

## LocalizedAssetEvent

LocalizeAssetEvent can localize a Unity asset, such as a Texture or Audio clip. LocalizedAssetEventis an abstract class that should be inherited from in order to set the Asset type. The Localization System currently features some implemented versions of LocalizedAssetBehaviour for **Texture(LocalizeTextureEvent)** and **Audio Clip(LocalizeAudioClipEvent)**.

![Localize Texture Editor.](images/Component_LocalizeTexture.png)

This example shows how support could be added for the Font asset.

[!code-cs[source-order](../DocCodeSamples.Tests/LocalizedFontComponent.cs#sample-code)]

## Automatic Setup

Some Unity components allow you to add an automatic Component Localizer to them. These components have a **Localize** option in their context menus. When you select this, Unity adds a new Component Localizer and hook its events up to the target component.

The following components support automatic setup:

- [Text](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html)
- [RawImage](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-RawImage.html)
- [Image](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html)
- [Text - TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/TMPObjectUIText.html)
- [AudioSource](https://docs.unity3d.com/Manual/class-AudioSource.html)

![Localize Component menu.](images/Component_LocalizeMenu.png)

![Localize String Component.](images/Component_LocalizeString.png)

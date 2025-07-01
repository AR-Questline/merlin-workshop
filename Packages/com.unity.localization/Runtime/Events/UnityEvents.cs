using System;
using UnityEngine.Events;

namespace UnityEngine.Localization.Events
{
    #if MODULE_AUDIO || PACKAGE_DOCS_GENERATION
    /// <summary>
    /// [UnityEvent](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html) which can pass an [AudioClip](https://docs.unity3d.com/ScriptReference/AudioClip.html) as an argument.
    /// </summary>
    [Serializable]
    public class UnityEventAudioClip : UnityEvent<AudioClip> {}
    #endif

    /// <summary>
    /// [UnityEvent](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html) which can pass a [GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html) as an argument.
    /// </summary>
    [Serializable]
    public class UnityEventGameObject : UnityEvent<GameObject> {}

    /// <summary>
    /// [UnityEvent](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html) which can pass a [Sprite](https://docs.unity3d.com/ScriptReference/Sprite.html) as an argument.
    /// </summary>
    [Serializable]
    public class UnityEventSprite : UnityEvent<Sprite> {}

    /// <summary>
    /// [UnityEvent](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html) which contains the Localized String as an argument.
    /// </summary>
    [Serializable]
    public class UnityEventString : UnityEvent<string> {};

    /// <summary>
    /// [UnityEvent](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html) which can pass an [Texture](https://docs.unity3d.com/ScriptReference/Texture.html) as an argument.
    /// </summary>
    [Serializable]
    public class UnityEventTexture : UnityEvent<Texture> {}
}

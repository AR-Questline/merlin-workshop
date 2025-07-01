namespace Samples.LocalizedFontEventComponent
{
    #region sample-code

    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Components;

    [Serializable]
    public class LocalizedFont : LocalizedAsset<Font> {}

    [Serializable]
    public class FontEvent : UnityEvent<Font> {}

    public class LocalizedFontEventComponent : LocalizedAssetEvent<Font, LocalizedFont, FontEvent> {}
    #endregion
}

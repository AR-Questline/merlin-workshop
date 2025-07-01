#if PACKAGE_UGUI

namespace Samples.LocalizedFontComponent
{
    #region sample-code

    using System;
    using UnityEngine;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Components;
    using UnityEngine.UI;

    [Serializable]
    public class LocalizedFont : LocalizedAsset<Font> {}

    public class LocalizedFontComponent : LocalizedAssetBehaviour<Font, LocalizedFont>
    {
        public Text text;

        protected override void UpdateAsset(Font localizedFont)
        {
            text.font = localizedFont;
        }
    }
    #endregion
}

#endif

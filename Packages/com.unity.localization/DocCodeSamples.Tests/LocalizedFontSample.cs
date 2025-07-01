using System;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class LocalizedFont : LocalizedAsset<Font> {}

public class LocalizedFontExample : MonoBehaviour
{
    // Can be setup in the inspector
    public LocalizedFont myFont;

    public void OnEnable()
    {
        myFont.AssetChanged += FontChanged;
    }

    public void OnDisable()
    {
        myFont.AssetChanged -= FontChanged;
    }

    void FontChanged(Font font)
    {
        // Do something with the font
    }
}

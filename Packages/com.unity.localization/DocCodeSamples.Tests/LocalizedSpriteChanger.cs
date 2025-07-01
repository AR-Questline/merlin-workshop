#if PACKAGE_UGUI

#region example-code

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class LocalizedSpriteChanger : MonoBehaviour
{
    public LocalizeSpriteEvent localizeSpriteEvent;
    public LocalizedSprite[] sprites;
    public Image image;
    int currentSprite = 0;

    private void Start()
    {
        ChangeSprite(sprites[currentSprite]);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Previous"))
        {
            if (currentSprite == 0)
                currentSprite = sprites.Length - 1;
            else
                currentSprite--;
            ChangeSprite(sprites[currentSprite]);
        }

        // Show the current sprite that is visible
        GUILayout.Label(image.sprite?.name);

        if (GUILayout.Button("Next"))
        {
            if (currentSprite == sprites.Length - 1)
                currentSprite = 0;
            else
                currentSprite++;
            ChangeSprite(sprites[currentSprite]);
        }

        GUILayout.EndHorizontal();
    }

    void ChangeSprite(LocalizedSprite sprite)
    {
        // When we assign a new AssetReference the system will automatically load the new Sprite asset then call the AssetChanged event.
        localizeSpriteEvent.AssetReference = sprite;
    }
}

#endregion

#endif

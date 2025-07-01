#if PACKAGE_UGUI

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class LocalizedTextureChanger : MonoBehaviour
{
    public LocalizeTextureEvent localizeTextureEvent;
    public LocalizedTexture[] textures;
    public RawImage image;
    int currentTexture = 0;

    private void Start()
    {
        ChangeTexture(textures[currentTexture]);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Previous"))
        {
            if (currentTexture == 0)
                currentTexture = textures.Length - 1;
            else
                currentTexture--;
            ChangeTexture(textures[currentTexture]);
        }

        // Show the current texture that is visible
        GUILayout.Label(image.texture?.name);

        if (GUILayout.Button("Next"))
        {
            if (currentTexture == textures.Length - 1)
                currentTexture = 0;
            else
                currentTexture++;
            ChangeTexture(textures[currentTexture]);
        }

        GUILayout.EndHorizontal();
    }

    void ChangeTexture(LocalizedTexture texture)
    {
        // When we assign a new AssetReference the system will automatically load the new Sprite asset then call the AssetChanged event.
        localizeTextureEvent.AssetReference = texture;
    }
}

#endif

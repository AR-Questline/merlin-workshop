#if MODULE_AUDIO

#region release-asset-example
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class ReleaseAssetExample : MonoBehaviour
{
    public LocalizedAudioClip localizedAudioClip = new LocalizedAudioClip { TableReference = "My Table", TableEntryReference = "My Audio Clip" };
    public AudioSource audioSource;

    bool isLoadingAndPlaying;

    private void OnGUI()
    {
        if (isLoadingAndPlaying)
        {
            GUILayout.Label("Loading & Playing Clip");
            return;
        }

        if (GUILayout.Button("Load & Play Audio Clip"))
        {
            StartCoroutine(LoadAndPlay());
        }
    }

    IEnumerator LoadAndPlay()
    {
        isLoadingAndPlaying = true;

        var clipOperation = localizedAudioClip.LoadAssetAsync();

        // Acquire the operation. If another part of code was to call ReleaseAsset this would
        // prevent the asset from being unloaded whilst we are still using it.
        Addressables.ResourceManager.Acquire(clipOperation);

        // Wait for the clip to load.
        yield return clipOperation;

        // Play the clip.
        audioSource.clip = clipOperation.Result;
        audioSource.Play();

        // Wait for the clip to finish.
        yield return new WaitForSeconds(clipOperation.Result.length);

        // Release our handle
        audioSource.clip = null;
        Addressables.Release(clipOperation);

        // Get the asset table
        var table = LocalizationSettings.AssetDatabase.GetTable(localizedAudioClip.TableReference);

        // Tell the Asset Table to release the cached version. The asset will now
        // be unloaded as long as there are no other references.
        table.ReleaseAsset(localizedAudioClip.TableEntryReference);

        isLoadingAndPlaying = false;
    }
}
#endregion
#endif

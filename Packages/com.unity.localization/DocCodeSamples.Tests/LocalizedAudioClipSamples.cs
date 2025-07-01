#if MODULE_AUDIO

#region example
using UnityEngine;
using UnityEngine.Localization;

public class LocalizedAudioClipExample : MonoBehaviour
{
    public AudioSource audioSource;
    public LocalizedAudioClip localizedAudioClip = new LocalizedAudioClip
    {
        TableReference = "My Audio Table",
        TableEntryReference = "My Audio Clip",
    };

    void OnEnable()
    {
        // Starts loading the audio clip asynchronously.
        localizedAudioClip.AssetChanged += AudioAssetChanged;
    }

    void OnDisable()
    {
        localizedAudioClip.AssetChanged -= AudioAssetChanged;
    }

    /// <summary>
    /// Changes the audio clip to the one specified by the <see cref="audioName"/>.
    /// </summary>
    /// <param name="soundName"></param>
    public void PlaySound(string soundName)
    {
        // This will trigger an automatic update
        localizedAudioClip.TableEntryReference = soundName;
    }

    void AudioAssetChanged(AudioClip value)
    {
        audioSource.clip = value;
        audioSource.Play();
    }
}
#endregion

#endif

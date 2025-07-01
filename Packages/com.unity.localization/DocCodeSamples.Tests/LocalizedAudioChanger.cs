#if MODULE_AUDIO

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class LocalizedAudioChanger : MonoBehaviour
{
    public LocalizeAudioClipEvent localizeAudioClipEvent;
    public LocalizedAudioClip[] clips;
    public AudioSource audioSource;
    int currentClip = 0;

    private void Start()
    {
        // Start playing the first AudioClip.
        ChangeAudio(clips[currentClip]);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Previous"))
        {
            if (currentClip == 0)
                currentClip = clips.Length - 1;
            else
                currentClip--;
            ChangeAudio(clips[currentClip]);
        }

        // Show the current clip that is playing
        GUILayout.Label(audioSource.clip?.name);

        if (GUILayout.Button("Next"))
        {
            if (currentClip == clips.Length - 1)
                currentClip = 0;
            else
                currentClip++;
            ChangeAudio(clips[currentClip]);
        }

        GUILayout.EndHorizontal();
    }

    void ChangeAudio(LocalizedAudioClip clip)
    {
        // When we assign a new AssetReference the system will automatically load the new Audio clip and then call the AssetChanged event.
        localizeAudioClipEvent.AssetReference = clip;
    }
}

#endif

using System;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public class GenderAudioClipTemplate : ScriptableObject {
        [UnityEngine.Scripting.Preserve] public AudioClip[] maleClips = Array.Empty<AudioClip>();
        [UnityEngine.Scripting.Preserve] public AudioClip[] femaleClips = Array.Empty<AudioClip>();
    }
}
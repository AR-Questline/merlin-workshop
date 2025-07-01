using System;
using Awaken.Utility.Debugging;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.AudioSystem {
    public static class AudioManager {
        // === Mixer Settings
        public static void SetAudioChannelVolume(AudioGroup audioGroup, float volume) {
            // try {
            //     if (audioGroup.TryGetVCA(out var vca)) {
            //         vca.setVolume(volume);
            //     }
            // } catch (Exception e) {
            //     Log.Important?.Error($"Exception below happened while trying to set volume to VCA: {audioGroup}");
            //     Debug.LogException(e);
            //     // --- ignore
            // }
        }
        
        [UnityEngine.Scripting.Preserve]
        public static bool GetAudioChannelVolume(AudioGroup audioGroup, out float currentVolume) {
            currentVolume = 0;
            // try {
            //     if (audioGroup.TryGetVCA(out var vca)) {
            //         vca.getVolume(out currentVolume);
            //         return true;
            //     }
            //     return false;
            // } catch {
            //     // --- ignore
            //     return false;
            // }
            return false;
        }
    }
}

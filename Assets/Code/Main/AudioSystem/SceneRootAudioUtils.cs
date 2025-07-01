using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AudioSystem {
    public static class SceneRootAudioUtils {
        public static void InitializeSceneAudio(ref bool audioInitialized, bool interpolateCombatLevel, IAudioSource[] musicAudioSources, IAudioSource[] musicAlertAudioSources, IAudioSource[] musicCombatAudioSources, BaseAudioSource ambientAudioSource, BaseAudioSource snapshotAudioSource) {
            AudioCore audioCore = World.Services.Get<AudioCore>();
            audioCore.interpolateCombatLevel = interpolateCombatLevel;

            if (audioInitialized) {
                return;
            }
            audioInitialized = true;
            
            audioCore.RegisterAudioSources(musicAudioSources, AudioType.Music, false);
            audioCore.RegisterAudioSources(musicAlertAudioSources, AudioType.MusicAlert, false);
            audioCore.RegisterAudioSources(musicCombatAudioSources, AudioType.MusicCombat, false);
            if (!(ambientAudioSource?.EventReference().IsNull ?? true)) {
                audioCore.RegisterAudioSource(ambientAudioSource, AudioType.Ambient);
            }
            if (!(snapshotAudioSource?.EventReference().IsNull ?? true)) {
                audioCore.RegisterAudioSource(snapshotAudioSource, AudioType.Snapshot);
            }
        }
        
        public static void UnloadSceneAudio(ref bool audioInitialized, IAudioSource[] musicAudioSources, IAudioSource[] musicAlertAudioSources, IAudioSource[] musicCombatAudioSources, BaseAudioSource ambientAudioSource, BaseAudioSource snapshotAudioSource) {
            AudioCore audioCore = World.Services.TryGet<AudioCore>();
            if (audioCore != null) {
                audioCore.interpolateCombatLevel = true;
            }

            if (!audioInitialized) {
                return;
            }
            audioInitialized = false;
            
            if (audioCore == null) {
                return;
            }
            audioCore.UnregisterAudioSources(musicAudioSources, AudioType.Music);
            audioCore.UnregisterAudioSources(musicAlertAudioSources, AudioType.MusicAlert);
            audioCore.UnregisterAudioSources(musicCombatAudioSources, AudioType.MusicCombat);
            if (!(ambientAudioSource?.EventReference().IsNull ?? true)) {
                audioCore.UnregisterAudioSource(ambientAudioSource, AudioType.Ambient);
            }
            if (!(snapshotAudioSource?.EventReference().IsNull ?? true)) {
                audioCore.UnregisterAudioSource(snapshotAudioSource, AudioType.Snapshot);
            }
        }
    }
}
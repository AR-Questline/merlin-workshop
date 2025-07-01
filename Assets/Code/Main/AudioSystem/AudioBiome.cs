using System;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
// ReSharper disable UseArrayEmptyMethod

namespace Awaken.TG.Main.AudioSystem {
    /// <summary>
    /// Base audio biome class, inherit from this for boilerplate
    /// </summary>
    public class AudioBiome : MonoBehaviour {
        [SerializeReference] public IAudioSource[] _musicToRegister = new IAudioSource[0];
        [SerializeReference] public IAudioSource[] _alertMusicToRegister = new IAudioSource[0];
        [SerializeReference] public IAudioSource[] _combatMusicToRegister = new IAudioSource[0];
        [SerializeReference] public IAudioSource[] _ambientsToRegister = new IAudioSource[0];
        [SerializeReference] public IAudioSource[] _snapshotsToRegister = new IAudioSource[0];
        static AudioCore AudioCore => World.Services.Get<AudioCore>();

        void Awake() {
            Vector3 position = transform.position;
            _musicToRegister.ForEach(m => m.SetPosition(position));
            _alertMusicToRegister.ForEach(m => m.SetPosition(position));
            _combatMusicToRegister.ForEach(m => m.SetPosition(position));
            _ambientsToRegister.ForEach(m => m.SetPosition(position));
            _snapshotsToRegister.ForEach(m => m.SetPosition(position));
        }

        [Button]
        protected void ActivateBiome() {
            var audioCore = AudioCore;
            audioCore.RegisterAudioSources(_musicToRegister, AudioType.Music);
            audioCore.RegisterAudioSources(_alertMusicToRegister, AudioType.MusicAlert, false);
            audioCore.RegisterAudioSources(_combatMusicToRegister, AudioType.MusicCombat, false);
            audioCore.RegisterAudioSources(_ambientsToRegister, AudioType.Ambient);
            audioCore.RegisterAudioSources(_snapshotsToRegister, AudioType.Snapshot);
        }

        [Button]
        protected void DeactivateBiome() {
            var audioCore = AudioCore;
            audioCore.UnregisterAudioSources(_musicToRegister, AudioType.Music);
            audioCore.UnregisterAudioSources(_alertMusicToRegister, AudioType.MusicAlert);
            audioCore.UnregisterAudioSources(_combatMusicToRegister, AudioType.MusicCombat);
            audioCore.UnregisterAudioSources(_ambientsToRegister, AudioType.Ambient);
            audioCore.UnregisterAudioSources(_snapshotsToRegister, AudioType.Snapshot);
        }
    }
}
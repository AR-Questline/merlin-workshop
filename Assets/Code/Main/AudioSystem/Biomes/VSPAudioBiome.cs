using System.Collections.Generic;
using AwesomeTechnologies.VegetationSystem;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem.Biomes {
    [RequireComponent(typeof(BiomeMaskArea))]
    class VSPAudioBiome : AudioBiome {
        public IAudioSource[] GetAmbientSources => _ambientsToRegister;
        public IAudioSource[] GetMusicSources => _musicToRegister;
        public IAudioSource[] GetSnapshotSources => _snapshotsToRegister;
    }
}
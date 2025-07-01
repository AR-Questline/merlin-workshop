using System;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners {
    [Serializable]
    public class SpawnerRandomizationSettings {
        [MinValue(nameof(groupSpawnCap))]
        public int totalSpawnCap = 6;
        [Min(1)]
        public int groupSpawnCap = 3;
        [Min(1), GUIColor(0, 1, 1)]
        public float spawnRadius = 5;
        [MaxValue("@-1+" + nameof(spawnRadius)), GUIColor(1, 0.92f, 0.016f)]
        public float groupSpawnRadius = 5;

        [LabelText("Spawn Interval [s]"), Min(0.1f)]
        public float spawnInterval = 5;

        [Indent, SuffixLabel("interval * (1 +- variance)", Overlay = true), Min(0)]
        public float spawnIntervalVariance = 0.25f;

        [Space] [SerializeField] List<LocationTemplateRandomSpawn> locationsToSpawn = new();
        public bool shouldAlwaysSpawnSuccessfully = true;

        public IEnumerable<LocationTemplateRandomSpawn> RandomLocationsToSpawn => locationsToSpawn;

        [Serializable]
        public class LocationTemplateRandomSpawn {
            [InfoBox("Cannot be unique npc", InfoMessageType.Error, nameof(NotRepetitiveNpc))]
            [TemplateType(typeof(LocationTemplate)), HideLabel]
            public TemplateReference locationToSpawn;

            [Range(0, 1f)] public float spawnChancePerInterval = 0.5f;

            [Tooltip("0 - do not spawn, <0 - no cap"), OnValueChanged(nameof(SpawnCapValueChange))]
            public float spawnCap = float.PositiveInfinity;

            [HideInInspector] public int id;

            void SpawnCapValueChange() {
                if (spawnCap < 0) {
                    spawnCap = float.PositiveInfinity;
                }
            }
            
            bool NotRepetitiveNpc => RepetitiveNpcUtils.InvalidLocation(locationToSpawn);
        }
    }
}

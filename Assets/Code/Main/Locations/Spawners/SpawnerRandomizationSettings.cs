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
        [MinValue(nameof(Editor_ValidTotalSpawnCap))]
        public byte totalSpawnCap = 6;
        [MinValue(0), Indent(2)]
        public byte randomSpawnCapIncreaseAtInstantiation = 0;
        [MinValue(1)]
        public byte groupSpawnCap = 3;
        [Min(1), GUIColor(0, 1, 1)]
        public float spawnRadius = 5;
        [MaxValue(nameof(spawnRadius)), GUIColor(1, 0.92f, 0.016f), SerializeField, ShowIf(nameof(GroupSettingsMatter))]
        float groupSpawnRadius = 5;

        [LabelText("Spawn Interval [s]"), Min(0.1f), ShowIf(nameof(GroupSettingsMatter))]
        public float spawnInterval = 5;

        [Indent, SuffixLabel("interval * (1 +- variance)", Overlay = true), Min(0), ShowIf(nameof(GroupSettingsMatter))]
        public float spawnIntervalVariance = 0.25f;

        [Space] [SerializeField] List<LocationTemplateRandomSpawn> locationsToSpawn = new();
        public bool shouldAlwaysSpawnSuccessfully = true;
        public Vector3 spawnOffsetFromSpawner = Vector3.zero;
        public bool skipSnapToGround = false;

        public IEnumerable<LocationTemplateRandomSpawn> RandomLocationsToSpawn => locationsToSpawn;
        
        int Editor_ValidTotalSpawnCap => groupSpawnCap - randomSpawnCapIncreaseAtInstantiation;
        public bool GroupSettingsMatter => groupSpawnCap < totalSpawnCap + randomSpawnCapIncreaseAtInstantiation;

        public float GroupSpawnRadius => GroupSettingsMatter ? groupSpawnRadius : spawnRadius;

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

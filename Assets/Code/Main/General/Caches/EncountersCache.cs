using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.General.Caches {
    public class EncountersCache : BaseCache, ISceneCache<SceneEncountersSources, EncounterData> {
        static EncountersCache s_cache;
        public static EncountersCache Get => s_cache = s_cache ? s_cache : LoadFromResources<EncountersCache>("Caches/EncountersCache");
        
        public List<SceneEncountersSources> encounters = new();
        
        public override void Clear() {
            encounters.Clear();
        }

        List<SceneEncountersSources> ISceneCache<SceneEncountersSources, EncounterData>.Data => encounters;
    }

    [Serializable]
    public class SceneEncountersSources : SceneDataSources, ISceneCacheData<EncounterData> {
        [PropertyOrder(1)]
        [ListDrawerSettings(NumberOfItemsPerPage = 8, IsReadOnly = true, ShowFoldout = false, ListElementLabelName = nameof(EncounterData.Label))]
        public List<EncounterData> data = new();

        public SceneEncountersSources(SceneReference sceneRef) : base(sceneRef) { }
        
        SceneReference ISceneCacheData<EncounterData>.SceneRef=> sceneRef;
        List<EncounterData> ISceneCacheData<EncounterData>.Sources => data;
    }

    [Serializable]
    public class EncounterData : ISceneCacheSource, IEquatable<EncounterData> {
        const float SpawnDistanceOffset = 3f;
        [ListDrawerSettings(IsReadOnly = true, ListElementLabelName = nameof(EnemyWithPos.NPCName))]
        public List<EnemyWithPos> npcs = new();
        
        [ShowInInspector]
        public float DifficultyScore => npcs.Sum(npc => npc.difficultyScore);

        string _label;
        public string Label => string.IsNullOrEmpty(_label) ? _label = GetLabel() : _label;
        
        [field: SerializeField, ReadOnly, ShowInInspector]
        public string Whereabouts { get; set; }
        
        [Button, ShowIf(nameof(CanSpawnEncounter))]
        void SpawnEncounter() {
            Vector3 spawnPosition = AstarPath.active.GetNearest(Hero.Current.Coords + Hero.Current.Forward() * SpawnDistanceOffset).position;
            int halfOfNpcs = npcs.Count / 2;
            Vector3 heroRight = Hero.Current.ParentTransform.right;
            Vector3 positionOffset = -heroRight * SpawnDistanceOffset * halfOfNpcs;
            
            foreach (var enemy in npcs) {
                Location loc = enemy.npc.LocationTemplate.SpawnLocation(spawnPosition + positionOffset, Random.rotation);
                RepetitiveNpcUtils.Check(loc);
                positionOffset += heroRight * SpawnDistanceOffset;
            }
        }

        string GetLabel() {
            return $"Count: {npcs.Count}, difficulty: {DifficultyScore}";
        }

        bool CanSpawnEncounter() {
            return Hero.Current != null;
        }

        public bool Equals(EncounterData other) {
            return ReferenceEquals(this, other);
        }
    }
    
    [Serializable]
    public struct EnemyWithPos {
        public Vector3 pos;
        public NpcSource npc;
        public float difficultyScore;

        string _npcName;
        public string NPCName => string.IsNullOrEmpty(_npcName) ? _npcName = npc.NpcTemplate.name : _npcName;
            
        public EnemyWithPos(NpcSource npc, Vector3 pos, float difficultyScore) {
            this.npc = npc;
            this.pos = pos;
            this.difficultyScore = difficultyScore;
            _npcName = null;
        }
    }
}
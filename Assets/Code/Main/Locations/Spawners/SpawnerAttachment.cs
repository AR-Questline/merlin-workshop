using System;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Utility.Tags;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;

namespace Awaken.TG.Main.Locations.Spawners {
    public abstract class SpawnerAttachment : MonoBehaviour {
        protected const float DefaultSpawnerCooldown = 7200f; //2 hours
        protected const string SpawnAvailabilityGroup = "Spawn Availability";
        public bool manualSpawner;
        [HideIf(nameof(manualSpawner))] bool canTriggerAmbush = true;
        [ShowIf(nameof(CanTriggerAmbush))] public bool spawnOnlyOnAmbush;
        
        public bool discardSpawnedLocationsOnDiscard;
        [FoldoutGroup(SpawnAvailabilityGroup)]
        public bool spawnOnlyAtNight;
        [FoldoutGroup(SpawnAvailabilityGroup)]
        public bool useFlagAvailability;
        [FoldoutGroup(SpawnAvailabilityGroup), ShowIf(nameof(useFlagAvailability)), InlineProperty, HideLabel]
        public FlagLogic availability;
        [Title("Story"), ShowIf(nameof(ShowStoryOnAllKilled)), PropertyOrder(99)]
        public StoryBookmark storyOnAllKilled;
        
        public bool CanTriggerAmbush => !manualSpawner && canTriggerAmbush;
        
        // === Odin
        protected virtual bool ShowStoryOnAllKilled => false;
        
    }
}

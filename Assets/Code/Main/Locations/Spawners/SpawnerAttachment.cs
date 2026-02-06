using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

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
        
        [Title("Story"), ShowIf(nameof(ShowStoryOnAllKilled)), SerializeField, PropertyOrder(99)]
        StoryBookmark storyOnAllKilled;
        
        [Title("Status to apply to spawned units"), SerializeField, PropertyOrder(100), TemplateType(typeof(StatusTemplate))]
        TemplateReference statusToApply;
        [SerializeField, PropertyOrder(101), HideIf(nameof(DoesNotHaveStatusToApply))] int durationOverride = -1;
        [SerializeField, PropertyOrder(102), HideIf(nameof(DoesNotHaveStatusToApply))] int buildupStrength = 1;
        
        public bool CanTriggerAmbush => !manualSpawner && canTriggerAmbush;
        public StoryBookmark StoryOnAllKilled => ShowStoryOnAllKilled ? storyOnAllKilled : null;
        
        bool DoesNotHaveStatusToApply => statusToApply is null || !statusToApply.IsSet;

        public StatusToApplySettings StatusToApply {
            get {
                if (statusToApply?.TryGet<StatusTemplate>(this) is not { } status) {
                    return null;
                }
                return new StatusToApplySettings {
                    status = status,
                    durationOverride = durationOverride,
                    buildupStrength = buildupStrength,
                };
            }
        }

        // === Odin
        protected virtual bool ShowStoryOnAllKilled => false;
    }
    
    public class StatusToApplySettings {
        public StatusTemplate status;
        public int durationOverride;
        public int buildupStrength;
    }
}

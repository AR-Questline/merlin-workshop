using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Main.AI.Combat.CustomDeath {
    [Serializable]
    public class CustomDeathAnimation : IDeathAnimationProvider {
        [SerializeField, AnimancerAnimationsAssetReference] public ShareableARAssetReference animations;
        [field: SerializeReference] public List<ICustomDeathAnimationConditions> conditions = new();
        [SerializeField] bool useCustomRagdollData;
        [SerializeField, ShowIf(nameof(useCustomRagdollData))] PostponedRagdollBehaviourBase.RagdollEnableData customRagdollData = PostponedRagdollBehaviourBase.RagdollEnableData.Default;
        ARCustomDeathAnimations _arInteractionAnimations;
        AsyncOperationHandle<ARStateToAnimationMapping> _preloadedAnimations;

        public bool UseCustomRagdollData => useCustomRagdollData;
        public PostponedRagdollBehaviourBase.RagdollEnableData CustomRagdollData => customRagdollData;
        public bool CanPlayAnimationAfterLoad => !useCustomRagdollData || !customRagdollData.enableRagdollAfterAnimation;
        
        public bool CheckIfLoaded() {
            if (_arInteractionAnimations == null) return false;
            if (_arInteractionAnimations.IsLoadingOverrides) {
                return false;
            } 
            return true;
        }

        public bool CheckConditions(DamageOutcome damageOutcome, bool isValidationCheck = false) {
            return conditions.All(c => c.Check(damageOutcome, isValidationCheck));
        }

        public void Preload() {
            if (_preloadedAnimations.IsValid()) {
                return;
            }
            
            _preloadedAnimations = animations.PreloadLight<ARStateToAnimationMapping>();
        }

        public void UnloadPreload() {
            if (_preloadedAnimations.IsValid() == false) {
                return;
            }
            animations.ReleasePreloadLight(_preloadedAnimations);
            _preloadedAnimations = default;
        }

        public void Load(NpcElement npc) {
            Load(npc?.Controller?.ARNpcAnimancer);
        }

        public void Load(ARNpcAnimancer npcAnimancer) {
            _arInteractionAnimations = new ARCustomDeathAnimations(npcAnimancer, animations);
            _arInteractionAnimations.LoadOverride();
        }

        public void Apply() {
            _arInteractionAnimations.ApplyOverrides();
        }

        public void UnloadAndClear() {
            _arInteractionAnimations?.UnloadOverride();
            _arInteractionAnimations = null;
        }
    }
}

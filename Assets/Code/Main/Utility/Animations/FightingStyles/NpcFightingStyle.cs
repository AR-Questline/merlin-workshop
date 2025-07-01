using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Attachments.Humanoids;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using UnityEditor;
#endif

namespace Awaken.TG.Main.Utility.Animations.FightingStyles {
    [Serializable]
    public abstract class NpcFightingStyle : ScriptableObject, ITemplate {
        public float desiredDistanceToTarget = VHeroCombatSlots.FirstLineCombatSlotOffset;
        public float desiredDistanceToTargetWhenFistFighting = 1.25f;
        public float minDistanceToTargetWhenFistFighting = 1.25f;
        
        [FoldoutGroup("Animation Avatar Masks")] public AvatarMask generalMask;
        [FoldoutGroup("Animation Avatar Masks")] public AvatarMask customActionsMask;
        [FoldoutGroup("Animation Avatar Masks")] public AvatarMask additiveMask;
        [FoldoutGroup("Animation Avatar Masks")] public AvatarMask topBodyMask;
        [FoldoutGroup("Animation Avatar Masks")] public AvatarMask overridesMask;
        [FoldoutGroup("Animation Avatar Masks")] public bool areOverridesAdditive;
        
        [Required]
        [SerializeField, AnimancerAnimationsAssetReference]
        [InfoBox("This animations are used as fallback, they are not used when randomizing attack from additional animations. " +
                 "Here mostly should be animations like outside combat locomotion, idle, etc.")]
        ShareableARAssetReference baseAnimations;
        
        [Required]
        [SerializeField, EnemyBehavioursAssetReference]
        ShareableARAssetReference baseBehaviours;
        
        [SerializeField, HideInInspector] TemplateMetadata metadata;
        
        public ShareableARAssetReference BaseAnimations => baseAnimations;
        public ShareableARAssetReference BaseBehaviours => baseBehaviours;
        public abstract AnimationAndBehaviourMappingEntry RetrieveCombatData(EnemyBaseClass enemyBaseClass);
        public abstract AnimationAndBehaviourMappingEntry RetrieveCombatData(FighterType fighterType);
        
        // === ITemplate
        public string GUID { get; set; }
        public TemplateMetadata Metadata => metadata;
        
        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;

        // Validation
#if UNITY_EDITOR
        public ShareableARAssetReference EDITOR_baseAnimations {
            get => baseAnimations;
            set => baseAnimations = value;
        }
        public ShareableARAssetReference EDITOR_baseBehaviours {
            get => baseBehaviours;
            set => baseBehaviours = value;
        }
        [Button]
        public void Validate() {
            int errorsCount = 0;
            ValidateBaseAssets(ref errorsCount);
            ValidateAdditionalAssets(ref errorsCount);

            if (errorsCount > 0) {
                Log.Important?.Error($"Validation of asset: {this} ended with {errorsCount} errors.", this);
            } else {
                Log.Important?.Info($"Validation of asset {this} ended, no errors found.", this);
            }
        }

        protected virtual void ValidateBaseAssets(ref int errorsCount) {
            if (baseBehaviours == null) {
                errorsCount++;
                Log.Important?.Error($"Null base behaviours! This is game breaking bug! Fix immediately! {this}", this);
                return;
            }
            
            Dictionary<string, ARStateToAnimationMapping> animationMappings = new();
            string path = AssetDatabase.GUIDToAssetPath(baseAnimations.AssetGUID);
            ARStateToAnimationMapping animationMapping = AssetDatabase.LoadAssetAtPath<ARStateToAnimationMapping>(path);
            animationMappings[path] = animationMapping;

            if (animationMapping == null) {
                errorsCount++;
                Log.Important?.Error($"Null base animation mapping! This is game breaking bug! Fix immediately! {this}", this);
                return;
            }
            
            InternalValidateCombatBehaviours(baseBehaviours, animationMappings, ref errorsCount);
        }
        
        protected abstract void ValidateAdditionalAssets(ref int errorsCount);

        protected void ValidateAsset(AnimationAndBehaviourMappingEntry mapping, ref int errorsCount, params ShareableARAssetReference[] additionalBehaviours) {
            Dictionary<string, ARStateToAnimationMapping> animationMappings = new();
            
            foreach ((_, List<ShareableARAssetReference> anims) in mapping.GetAllAnimations()) {
                AddAnimationMapping(baseAnimations);
                if (anims.Count > 0) {
                    anims.ForEach(AddAnimationMapping);
                }

                foreach (var m in animationMappings.Values) {
                    foreach (var entry in m.entries) {
                        if (!entry.IsMixerType && entry.clipTransitions.Any(c => c.Clip == null)) {
                            Log.Important?.Error($"Null Clip for state: {entry.npcStateType} assigned in {m}", m);
                        }
                    }
                }
                
                foreach (var combatBehaviourMapping in mapping.CombatBehaviours.Concat(additionalBehaviours)) {
                    InternalValidateCombatBehaviours(combatBehaviourMapping, animationMappings, ref errorsCount);
                }
                
                animationMappings.Clear();

                void AddAnimationMapping(ShareableARAssetReference animationMappingRef) {
                    string path = AssetDatabase.GUIDToAssetPath(animationMappingRef.AssetGUID);
                    ARStateToAnimationMapping animationMapping = AssetDatabase.LoadAssetAtPath<ARStateToAnimationMapping>(path);
                    animationMappings.Add(path, animationMapping);
                }
            }
        }

        void InternalValidateCombatBehaviours(ShareableARAssetReference combatBehaviourMapping, Dictionary<string, ARStateToAnimationMapping> animationMappings, ref int errorsCount) {
            string assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this));
            
            string path = AssetDatabase.GUIDToAssetPath(combatBehaviourMapping.AssetGUID);
            AREnemyBehavioursMapping behavioursMapping = AssetDatabase.LoadAssetAtPath<AREnemyBehavioursMapping>(path);
            string behavioursMappingName = Path.GetFileNameWithoutExtension(path);

            if (animationMappings.Count == 0) {
                errorsCount++;
                Log.Important?.Error($"In <b><size=12em>{assetName}</size></b> there are no animations assigned to behaviour: <b><size=12em>{behavioursMappingName}</size></b>", this);
                return;
            }
            
            foreach (var combatBehaviour in behavioursMapping.CombatBehaviours) {
                var accessor = combatBehaviour.GetEditorAccessor();
                
                foreach (NpcStateType stateType in accessor.StatesUsedByThisBehaviour) {
                    if (stateType == NpcStateType.None) {
                        continue;
                    }

                    
                    bool found = false;
                    foreach ((string animPath, ARStateToAnimationMapping animMapping) in animationMappings) {
                        if (animMapping.GetAnimancerNodes(stateType).Any()) {
                            found = true;
                        }
                    }

                    if (!found) {
                        string error = $"In <b><size=120%>{assetName}</size></b> For state <b><size=130%>{stateType}</size></b> used by <b><size=120%>{behavioursMappingName}</size></b> there is no animation mapping in any of the animation mappings! AnimationMappings attached: ";
                        foreach ((string animPath, ARStateToAnimationMapping animMapping) in animationMappings) {
                            string animMappingName = Path.GetFileNameWithoutExtension(animPath);
                            error += $"<b><size=120%>{animMappingName},</size></b> ";
                        }
                        Log.Important?.Error(error, this);
                        errorsCount++;
                    }
                }
            }
        }
#endif
    }
}
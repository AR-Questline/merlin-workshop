using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Block;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Unity.Mathematics;

namespace Awaken.TG.Main.Utility.Animations {
    public static class AnimancerUtils {
        const float FppBlendSpeed = 5f;
        const float TppBlendSpeed = 25f;
        const float Tolerance = 0.001f;
        static readonly ITransition[] EmptyTransitions = Array.Empty<ITransition>();

        public static IEnumerable<ITransition> GetAnimancerNodes(NpcStateType npcStateType, List<ARStateToAnimationMappingEntry> entries) {
            for (int i = 0; i < entries.Count; i++) {
                var entry = entries[i];
                if (entry.npcStateType == npcStateType) {
                    return entry.AnimancerNodes ?? EmptyTransitions;
                }
            }
            return EmptyTransitions;
        }
        
        public static IEnumerable<ITransition> GetAnimancerNodes(HeroStateType heroStateType, ARHeroStateToAnimationMapping mapping) {
            ARHeroStateToAnimationMappingEntry entry = mapping.entries.FirstOrDefault(e => e.heroStateType == heroStateType);
            return entry?.AnimancerNodes ?? EmptyTransitions;
        }

        public static bool IsMixerType(NpcStateType stateType) => stateType is NpcStateType.Movement
            or NpcStateType.AlertMovement or NpcStateType.CombatMovement or NpcStateType.ShieldManMovement or NpcStateType.FearMovement;

        public static bool IsMixerType(HeroStateType stateType) => stateType is HeroStateType.Movement
            or HeroStateType.MovementAlternate or HeroStateType.BlockLoop or HeroStateType.BlockLoopWithoutShield
            or HeroStateType.HeavyAttackWait or HeroStateType.HeavyAttackWaitAlternate or HeroStateType.FishingFight 
            or HeroStateType.CrouchedMovement or HeroStateType.HorseRidingMovement or HeroStateType.InAttackMovement 
            or HeroStateType.LegsSwimmingMovement;

        public static bool IsInTransition(this AnimancerLayer animancerLayer) {
            var currentState = animancerLayer.CurrentState;
            return currentState != null && math.abs(currentState.Weight - currentState.TargetWeight) > Tolerance;
        }

        public static float SynchronizeNormalizedTime(AnimancerState animancerState, float deltaTime) {
            return (animancerState.Time + deltaTime * animancerState.Speed) / animancerState.Length;
        }

        public static float BlendTreeBlendSpeed() {
            return Hero.TppActive ? TppBlendSpeed : FppBlendSpeed;
        }
    }
}
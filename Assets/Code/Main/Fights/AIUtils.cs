using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.IL2CPP.CompilerServices;
using Unity.Profiling;
using Unity.VisualScripting;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Fights {
    [Il2CppEagerStaticClassConstruction]
    public static class AIUtils {
        public static readonly UniversalProfilerMarker CanSeeMarker = new("AIUtils.CanSee");

        public const float HeroHalfHorizontalFov = 105 / 2f;
        public const float HeroHalfHorizontalPerception = 210 / 2f;
        const float FightingNotifyDelay = 0.8f;
        
        public static readonly float CosHeroHalfHorizontalPerception = Mathf.Cos(Mathf.Deg2Rad * HeroHalfHorizontalPerception);
        static readonly float CosHeroHalfHorizontalFoV = Mathf.Cos(Mathf.Deg2Rad * HeroHalfHorizontalFov);

        // === Battlefield Scanning
        
        public static ModelsSet<ICharacter>.WhereEnumerator FindEnemies(this ICharacter me) {
            ICharacter dummySkillCharacter = DummySkillCharacter.Instance;
            ModelsSet<ICharacter> characters = World.All<ICharacter>();
            return characters.Where(character => character != dummySkillCharacter && character != me && me.IsHostileTo(character));
        }

        public static ModelsSet<ICharacter>.WhereEnumerator FindAllies(this ICharacter me) {
            ICharacter dummySkillCharacter = DummySkillCharacter.Instance;
            ModelsSet<ICharacter> characters = World.All<ICharacter>();
            return characters.Where(character => character != dummySkillCharacter && character != me && me.IsFriendlyTo(character));
        }

        public static ModelsSet<ICharacter>.WhereEnumerator ValidCharacters() {
            ICharacter dummySkillCharacter = DummySkillCharacter.Instance;
            ModelsSet<ICharacter> characters = World.All<ICharacter>();
            return characters.Where(c => c != dummySkillCharacter);
        }

        public static IEnumerable<T> InRange<T>(this IEnumerable<T> grounded, Vector3 center, float range) where T : IGrounded {
            float rangeSqr = range * range;
            return grounded.Where(g => (g.Coords - center).sqrMagnitude < rangeSqr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRange(Vector3 center, Vector3 target, float range) {
            float rangeSqr = range * range;
            return (target - center).sqrMagnitude < rangeSqr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInCone(Vector3 startPoint, Vector3 targetPoint, Vector3 forward, float range, float maxAngle) {
            if (!IsInRange(startPoint, targetPoint, range)) {
                return false;
            }
            var direction = (targetPoint - startPoint).normalized;
            return Vector3.Dot(forward, direction) >= Mathf.Cos(maxAngle*Mathf.Deg2Rad);
        }
        
        public static bool IsRanged(this ICharacter character) {
            var inventory = character.Inventory;
            if (inventory == null) {
                return false;
            }

            foreach (var item in inventory.EquippedItems()) {
                if (item.IsRanged) {
                    return true;
                }
            }

            return false;
        }

        public const int NotBlockingAIVisionAndAI = NotBlockingAIVision | RenderLayers.Mask.AIs;
        public const int NotBlockingAIVision = RenderLayers.Mask.Transparent | 
                                               RenderLayers.Mask.IgnoreRaycast |
                                               RenderLayers.Mask.UI |
                                               RenderLayers.Mask.PostProcessing |
                                               RenderLayers.Mask.VFX |
                                               RenderLayers.Mask.MapMarker |
                                               RenderLayers.Mask.TriggerVolumes |
                                               RenderLayers.Mask.NavigationOnlyObjects |
                                               RenderLayers.Mask.RainObstacle |
                                               RenderLayers.Mask.RainTriggerVolume |
                                               RenderLayers.Mask.Impostor |
                                               RenderLayers.Mask.SafeZone |
                                               RenderLayers.Mask.Wyrdness |
                                               RenderLayers.Mask.Time |
                                               RenderLayers.Mask.Ragdolls |
                                               RenderLayers.Mask.ProjectileAndAttacks |
                                               RenderLayers.Mask.Hitboxes |
                                               RenderLayers.Mask.AIInteractions |
                                               RenderLayers.Mask.PlayerInteractions |
                                               RenderLayers.Mask.Player;
        
        public static VisibleState CanSee(this IAIEntity subject, IAIEntity target, bool throughAI = true) {
            int mask = NotBlockingAIVision;
            if (throughAI) {
                mask |= RenderLayers.Mask.AIs;
            }
            mask = ~mask;

            var visionDetectionSetups = target.VisionDetectionSetups;
            var canSee = visionDetectionSetups.Length > 0 ? VisibleState.Visible : VisibleState.Covered;
            for (int i = 0; i < visionDetectionSetups.Length; i++) {
                ref var visionDetectionSetup = ref visionDetectionSetups[i];
                var notSeeState = visionDetectionSetup.type == VisionDetectionTargetType.Main ?
                    VisibleState.Covered :
                    VisibleState.PartlyVisible;
                canSee = canSee.Union(subject.CanSee(target, visionDetectionSetup, mask) ? VisibleState.Visible : notSeeState);
            }
            return canSee;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CanSee(this IAIEntity subject, IAIEntity target, in VisionDetectionSetup detectionSetup, int blockViewMask = ~NotBlockingAIVision) {
            const float Step = 0.5f;

            CanSeeMarker.Begin();
            Vector3 origin = subject.VisionDetectionOrigin;
            Vector3 direction = detectionSetup.point - origin;
            float magnitude = direction.magnitude;
            if (magnitude <= 0.0001f) {
                CanSeeMarker.End();
                return true;
            }
            Vector3 normalizedDirection = direction / magnitude;
            bool hit;
            RaycastHit hitInfo;
            bool canSee;
            
            // --- Raycast
            if (detectionSetup.halfExtent < 0.01f) {
                do {
                    hit = Physics.Raycast(origin, direction, out hitInfo, magnitude, blockViewMask);
                    if (hit) {
                        Vector3 newOrigin = hitInfo.point + normalizedDirection * Step;
                        magnitude -= (origin - newOrigin).magnitude;
                        origin = newOrigin; 
                    }
                } while (magnitude > 0 && hit && IsEntity(hitInfo.collider.gameObject, subject));
                
                canSee = !hit || IsEntity(hitInfo.collider.gameObject, target);
                CanSeeMarker.End();
                return canSee;
            }
            
            // --- BoxCast
            var halfExtent = Vector3.one * detectionSetup.halfExtent;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            do {
                hit = Physics.BoxCast(origin, halfExtent, direction, out hitInfo, rotation, magnitude, blockViewMask);
                if (hit) {
                    origin = hitInfo.point + normalizedDirection * halfExtent.z;
                    magnitude -= halfExtent.z;
                }
            } while (magnitude > 0 && hit && IsEntity(hitInfo.collider.gameObject, subject));
            
            canSee = !hit || IsEntity(hitInfo.collider.gameObject, target);
            CanSeeMarker.End();
            return canSee;
        }
        
        static bool IsEntity(GameObject colliderHit, IAIEntity entity) {
            var alive = VGUtils.TryGetModel<IAlive>(colliderHit);
            return alive switch {
                null => false,
                NpcElement npc => npc.NpcAI == entity,
                _ => alive == entity
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanSee(Vector3 from, Vector3 to, int blockViewMask = ~NotBlockingAIVision) {
            Vector3 direction = to - from;
            return !Physics.Raycast(from, direction, direction.magnitude, blockViewMask);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqTo(this ICharacter character, ICharacter target) {
            return (character.Coords - target.Coords).sqrMagnitude;
        }

        // === Notifying
        public static async UniTaskVoid TryNotifyAboutOngoingFight(this NpcAI ai, ICharacter opponent) {
            if (!ai.InCombat) {
                return;
            }
            
            // Wait a bit to allow "sneaky" hits/kills
            if (!await AsyncUtil.DelayTime(ai, FightingNotifyDelay)) {
                return;
            }
            
            if (!ai.ParentModel.IsAlive || !ai.InCombat) {
                return;
            }

            var target = ai.ParentModel.GetCurrentTarget();
            if (target == null || target != opponent) {
                return;
            }
            NotifyAlliesAboutOngoingFight(ai, target);
        }
        
        public static void NotifyAlliesAboutFightStart(this NpcAI ai, ICharacter opponent) {
            var npcs = World.Services.Get<NpcGrid>().GetNotifiedNpcs(ai, ai.Data.perception.CoreInformRange, ai.Data.perception.MaxInformRange);
            foreach (var npc in npcs) {
                if (npc == ai.ParentModel) {
                    // don't notify themself
                    continue;
                } else if (CrimeReactionUtils.IsFleeing(npc)) {
                    // fleeing peasants use other system (grid danger) without notifications
                    continue;
                } else if (ai.ParentModel.IsFriendlyTo(npc)) {
                    npc.NpcAI.OngoingFightNextTo(opponent);
                }
            }
        }

        public static void NotifyAlliesAboutOngoingFight(this NpcAI ai, ICharacter opponent) {
            var npcs = World.Services.Get<NpcGrid>().GetNotifiedNpcs(ai, ai.Data.perception.CoreInformRange, ai.Data.perception.MaxInformRange);
            foreach (var npc in npcs) {
                if (CrimeReactionUtils.IsFleeing(npc)) {
                    // ignore
                } else if (ai.ParentModel.IsFriendlyTo(npc)) {
                    npc.NpcAI.OngoingFightNextTo(opponent);
                }
            }
        }
        
        static void OngoingFightNextTo(this NpcAI ai, ICharacter target) {
            ai.EnterCombatWith(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TryGetHeroVisibility(this NpcAI ai) {
            return ai?.HeroVisibility ?? 0;
        }

        // === Code <-> VS

        [UnityEngine.Scripting.Preserve]
        public static VariableDeclarations GetObjectVariables(this ICharacter character) {
            return character.CharacterView.transform.GetComponentInChildren<NpcController>().GetComponent<Variables>().declarations;
        }

        public static GameObject GetMachineGO(this ICharacter character) {
            return character switch {
                NpcElement npc => npc.ParentTransform != null ? npc.ParentTransform.gameObject : null,
                Hero hero => hero.VHeroController.gameObject,
                _ => null,
            };
        }

        // === Combat

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInCombat(this ICharacter character) {
            return character switch {
                NpcElement npc => npc.NpcAI?.InCombat ?? false,
                Hero hero => hero.HeroCombat.IsHeroInFight,
                null => throw new NullReferenceException(),
                _ => throw new NotImplementedException(),
            };
        }

        public static void RemoveAllNegativeStatusesFromCombatWithHero() {
            Hero.Current.Statuses.RemoveAllNegativeStatuses();
            foreach (var ai in NpcAI.AllWorkingAI) {
                ai.NpcElement.Statuses.RemoveAllNegativeStatuses();
            }
        }

        public static void ForceStopCombatWithHero() {
            ForceStopCombatWith(Hero.Current);
            foreach (var ally in Hero.Current.HeroCombat.Allies) {
                ForceStopCombatWith(ally);
            }
        }

        public static void ForceStopCombatWithHero(this NpcAI ai) {
            ai.NpcElement.ClearAllHostilityWith(AntagonismLayer.Default, Hero.Current);
            ai.ExitCombat(true, true, false);
            ai.AlertStack.RemovePoiOf(Hero.Current);
        }
        
        static void ForceStopCombatWith(ICharacter character) {
            foreach (var ai in NpcAI.AllWorkingAI) {
                ai.NpcElement.ClearAllHostilityWith(AntagonismLayer.Default, character);
                ai.ExitCombat(true, true);
                ai.AlertStack.RemovePoiOf(character);
            }
        }

        public static void RestoreItemsInHand(ref HashSet<GameObject> handActiveChildren) {
            foreach (GameObject child in handActiveChildren.Where(child => child != null)) {
                child.SetActive(true);
            }

            handActiveChildren.Clear();
        }

        public static void HideItemsInHand(Transform hand, ref HashSet<GameObject> handActiveChildren) {
            handActiveChildren.Clear();
            foreach (Transform child in hand) {
                if (child.gameObject.activeSelf) {
                    handActiveChildren.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }
        }

        // === Movement
        /// <summary>
        /// Limits deltaPosition to move towards target, but not further than minDistanceToTarget.
        /// </summary>
        /// <param name="currentTarget">Target we are heading towards</param>
        /// <param name="currentPosition">Our current position in 3D world</param>
        /// <param name="deltaPosition">How much we are going to move</param>
        /// <param name="minDistanceToTarget">How close to target we can be</param>
        /// <returns>Limited deltaPosition</returns>
        public static Vector3 LimitDeltaPositionTowardsTarget(ICharacter currentTarget, Vector3 currentPosition, Vector3 deltaPosition, float minDistanceToTarget) {
            // Value copied from Unity Vector3.Normalize();
            const float normalizeEpsilon = 9.99999974737875E-06f;
            
            if (currentTarget != null) {
                Vector3 targetPos = currentTarget.Coords.ToHorizontal3();
                Vector3 ourPos = currentPosition.ToHorizontal3();
                Vector3 directionToTarget = (targetPos - ourPos);
                // --- Calculate magnitude here to prevent twice calculation when normalizing vectors and then calculating distanceAfterMove.
                float deltaPositionMagnitude = deltaPosition.magnitude;
                float directionToTargetMagnitude = directionToTarget.magnitude;
                // --- Determine how much deltaPosition moves us towards Target
                Vector3 normalizedDeltaPosition = deltaPositionMagnitude > normalizeEpsilon ? deltaPosition / deltaPositionMagnitude : Vector3.zero;
                Vector3 directionToTargetNormalized = deltaPositionMagnitude > normalizeEpsilon ? directionToTarget / directionToTargetMagnitude : Vector3.zero;
                float towardsTargetDotProduct = Vector3.Dot(directionToTargetNormalized, normalizedDeltaPosition);
                // --- If deltaPosition moves us towards Target
                if (towardsTargetDotProduct > 0.5f) {
                    float towardsTargetMagnitude = deltaPositionMagnitude * towardsTargetDotProduct;
                    // --- How close to target we will be after moving
                    float distanceAfterMove = directionToTargetMagnitude - towardsTargetMagnitude;
                    // --- If we will be to close to Target after movement remove from deltaPosition movement towards Target
                    if (distanceAfterMove < minDistanceToTarget) {
                        float vectorLength = towardsTargetMagnitude <= 0.01f ? 1 : (minDistanceToTarget - distanceAfterMove) / towardsTargetMagnitude;
                        vectorLength = Mathf.Clamp(vectorLength, 0, 1);
                        deltaPosition -= directionToTargetNormalized * (towardsTargetMagnitude * vectorLength);
                    }
                }
            }
            return deltaPosition;
        }
        
        public static float HeroDotToTarget(Vector3 target) {
            Hero hero = Hero.Current;
            return HeroDotToTarget(target, hero.Coords, hero.Rotation);
        }
        
        public static float HeroDotToTarget(Vector3 target, Vector3 heroPosition, Quaternion heroRotation) {
            Vector3 heroForward = heroRotation * Vector3.forward;
            Vector3 directionToTarget = (target - heroPosition);
            return Vector3.Dot(heroForward, directionToTarget.normalized);
        }
        
        /// <param name="dotProduct">Dot Product of how much of hero forward is going towards direction to target</param>
        /// <returns>Returns if target is in Hero view cone</returns>
        public static bool IsInHeroViewCone(float dotProduct) {
            return dotProduct > CosHeroHalfHorizontalFoV;
        }
        
        /// <param name="dotProduct">Dot Product of how much of hero forward is going towards direction to target</param>
        /// <returns>Returns if target is behind Hero</returns>
        public static bool IsBehindHero(float dotProduct) {
            return dotProduct < CosHeroHalfHorizontalPerception;
        }

        public static bool HasUnreachablePathToHeroFromCombatSlotCondition(this EnemyBaseClass enemyBaseClass) {
            return enemyBaseClass.OwnedCombatSlotIndex != -1 &&
                   (Hero.Current.CombatSlots.HasReachablePathToHero(enemyBaseClass.OwnedCombatSlotIndex) == false || RichAIDoesntHavePath());
            
            bool RichAIDoesntHavePath() {
                var current = AstarPath.active.GetNearest(enemyBaseClass.Coords).node;
                RichAI richAI = enemyBaseClass.NpcMovement.Controller.RichAI;
                var destination = AstarPath.active.GetNearest(richAI.destination).node;
                if (current == null || destination == null) {
                    return true;
                }
                return !PathUtilities.IsPathPossible(current, destination);
            }
        }

        public static Optional<Vector3> FindBetterPositionForArcher(Vector3 basePosition, Vector3 targetPosition, int maxOffset) {
            for (int i = 1; i < maxOffset; i++) {
                if (AIUtils.CanSee(basePosition + Right * i, targetPosition)) {
                    return basePosition + Right * i;
                } else if (AIUtils.CanSee(basePosition + ForwardRight * i, targetPosition)) {
                    return basePosition + ForwardRight * i;
                } else if (AIUtils.CanSee(basePosition + ForwardLeft * i, targetPosition)) {
                    return basePosition + ForwardLeft * i;
                } else if (AIUtils.CanSee(basePosition - Right * i, targetPosition)) {
                    return basePosition - Right * i;
                } else if (AIUtils.CanSee(basePosition - ForwardRight * i, targetPosition)) {
                    return basePosition - ForwardRight * i;
                } else if (AIUtils.CanSee(basePosition - ForwardLeft * i, targetPosition)) {
                    return basePosition - ForwardLeft * i;
                } else if (AIUtils.CanSee(basePosition + Forward * i, targetPosition)) {
                    return basePosition + Forward * i;
                } else if (AIUtils.CanSee(basePosition - Forward * i, targetPosition)) {
                    return basePosition - Forward * i;
                }
            }
            return Optional<Vector3>.None;
        }
        
        public static async UniTask<SpawnedItemInHand> UseItemInHand(NpcElement npcElement, ShareableARAssetReference itemInHand, CancellationTokenSource cancellationToken) {
            HashSet<GameObject> hiddenItems = new();
            NpcStateType animatorStateType;
            Transform hand;
            if (npcElement.MainHand.childCount <= 0) {
                UseMainHand(false);
            } else if (npcElement.OffHand.childCount <= 0) {
                UseOffHand(false);
            } else {
                UseMainHand(true);
            }

            npcElement.SetAnimatorState(NpcFSMType.GeneralFSM, animatorStateType);
            cancellationToken ??= new CancellationTokenSource();
            IPooledInstance result = await PrefabPool.Instantiate(itemInHand, Vector3.zero, Quaternion.identity, hand, Vector3.one, cancellationToken.Token);
            return new SpawnedItemInHand(result, hiddenItems);
            
            void UseMainHand(bool hideItems) {
                hand = npcElement.MainHand;
                animatorStateType = NpcStateType.UseItemMainHand;
                if (hideItems) {
                    AIUtils.HideItemsInHand(hand, ref hiddenItems);
                }
            }

            void UseOffHand(bool hideItems) {
                hand = npcElement.OffHand;
                animatorStateType = NpcStateType.UseItemOffHand;
                if (hideItems) {
                    AIUtils.HideItemsInHand(hand, ref hiddenItems);
                }
            }
        }
        
        // === Helpers
        static readonly Vector3 Right = Vector3.right * 2.5f;
        static readonly Vector3 ForwardRight = (Vector3.right + Vector3.forward) * 1.5f;
        static readonly Vector3 ForwardLeft = (Vector3.right * -1 + Vector3.forward) * 1.5f;
        static readonly Vector3 Forward = Vector3.forward * 2.5f;
        
        public readonly struct SpawnedItemInHand {
            public IPooledInstance Instance { get; }
            public HashSet<GameObject> HiddenItems { get; }

            public SpawnedItemInHand(IPooledInstance instance, HashSet<GameObject> hiddenItems) {
                Instance = instance;
                HiddenItems = hiddenItems;
            }
        }
    }
}
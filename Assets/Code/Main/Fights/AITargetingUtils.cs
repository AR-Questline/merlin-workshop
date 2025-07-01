using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Combat;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Relations;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Fights {
    public static class AITargetingUtils {
        public const float BigFitDifference = 3f;
        public const float NormalFitDifference = 1f;
        public const float TakenDamageFitDifference = -1f;
        public const float ForceEndCombatDistanceSqr = 80f * 80f;
        
        static readonly int MaxEnemiesPerUnit = World.Services.Get<GameConstants>().maxEnemiesPerUnit;

        static readonly List<ICharacter> Ranged = new();
        static readonly List<ICharacter> Melee = new();
        
        [UnityEngine.Scripting.Preserve] static readonly EnumerableCache<ICharacter> CharacterCache = new(16);
        
        // === Targeting
        /// <summary>
        /// Gets Current Target, fast but ignores searching for new one if current one is invalid or null.
        /// </summary>
        public static ICharacter GetCurrentTarget(this NpcElement npc) {
            if (npc is not { NpcAI: { Working: true } }) {
                return null;
            }
            var target = npc.GetRelationsTarget();
            if (target != null && npc.IsStillValid(target)) {
                return target;
            }
            return null;
        }

        public static bool IsTargetingHero(this NpcElement npc) {
            if (npc is not { NpcAI: { Working: true } }) {
                return false;
            }
            var target = npc.GetRelationsTarget();
            var hero = Hero.Current;
            if (target != hero) {
                return false;
            }
            return IsStillValid(npc, hero);
        }
        
        /// <summary>
        /// Gets Current Target, fast but ignores searching for new one if current one is invalid or null.
        /// </summary>
        [Obsolete("Only npc's can target something. Unnecessary indirectness, use Npc version instead")]
        public static ICharacter GetCurrentTarget(this ICharacter character) {
            if (character is NpcElement npc) {
                return GetCurrentTarget(npc);
            }
            return null;
        }
        
        // === Targeting
        /// <summary>
        /// Gets Current Target even if not valid
        /// </summary>
        public static ICharacter ForceGetCurrentTarget(this NpcElement npc, out bool valid) {
            var target = npc.GetRelationsTarget();
            valid = target != null && npc.IsStillValidWithNpcChecks(target);
            return target;
        }

        /// <summary>
        /// Gets Current Target, if it's null than it checks surroundings looking for a new one.
        /// </summary>
        public static ICharacter GetOrSearchForTarget(this NpcElement npc) {
            var target = npc.GetRelationsTarget();
            if (target != null && npc.IsStillValidWithNpcChecks(target)) {
                return target;
            }

            return npc.GetAndUpdateNewTarget();
        }

        /// <summary>
        /// Gets Current Target, fast but ignores searching for new one if current one is invalid or null.
        /// </summary>
        public static bool TryGetCurrentTarget(this NpcElement npc, out ICharacter target) {
            target = npc.GetCurrentTarget();
            return target != null;
        }

        /// <summary>
        /// If target is valid and in range it adds it to list of possible targets
        /// </summary>
        public static bool TryAddPossibleCombatTarget(this NpcElement npc, ICharacter target, bool recalculateTarget = false) {
            if (npc.IsPossibleTarget(target)) {
                return npc.AddCombatTargetInternal(target, recalculateTarget);
            }
            return false;
        }
        
        /// <summary>
        /// Adds it to list of possible targets ignoring checks
        /// </summary>
        public static bool ForceAddCombatTarget(this NpcElement character, ICharacter target, bool recalculateTarget = false) {
            if (character.IsStillValidWithNpcChecks(target)) {
                return character.AddCombatTargetInternal(target, recalculateTarget);
            }
            return false;
        }

        static bool AddCombatTargetInternal(this NpcElement npc, ICharacter target, bool forceRecalculateTarget = false) {
            if (!npc.PossibleTargets.Add(target) && !forceRecalculateTarget) {
                return false;
            }

            if (forceRecalculateTarget) {
                npc.RecalculateTarget(force: true);
            } else {
                var currentTarget = npc.GetCurrentTarget();
                bool recalculateTarget = currentTarget == null || npc.IsBetterFitThanTarget(currentTarget, target, AITargetingUtils.BigFitDifference); 
                if (recalculateTarget) {
                    npc.RecalculateTarget();
                }
            }

            return true;
        }
        
        /// <summary>
        /// Removes character from all possible target and attackers list and nulls current target if it was the one.
        /// </summary>
        public static void ForceEndCombat(this ICharacter character) {
            character.PossibleAttackers.Clear();
            character.PossibleTargets.Clear();
            if (character is NpcElement npc) {
                npc.TryUpdateTarget(null);
            }
        }

        /// <summary>
        /// Removes character from possible targets. Searches for new Combat Target if it was current one.
        /// </summary>
        public static void RemoveCombatTarget(this NpcElement npc, ICharacter target, bool forceRecalculateTarget = false, bool wasInvalidTarget = false) {
            if (!npc.PossibleTargets.Contains(target)) {
                return;
            }
            
            bool wasTarget = target == npc.GetCurrentTarget();
            npc.PossibleTargets.Remove(target);
            
            if (wasTarget && !forceRecalculateTarget && !npc.GetPossibleTargets().Any()) {
                npc.TryUpdateTarget(null);
                return;
            }
            
            if (wasTarget || forceRecalculateTarget) {
                npc.RecalculateTarget();
            }
        }
        
        static ICharacter GetRelationsTarget(this NpcElement npc) {
            return npc.RelatedValue(Relations.Targets).Get();
        }
        
        /// <summary>
        /// List of enemies that are currently targeting this character.
        /// </summary>
        public static IEnumerable<ICharacter> GetTargeting(this ICharacter character) {
            return character.RelatedList(Relations.IsTargetedBy);
        }

        /// <summary>
        /// Checks if this enemy is currently targeting this character.
        /// </summary>
        public static bool IsTargetedBy(this NpcElement npc, ICharacter target) {
            return npc.GetTargeting().Contains(target);
        }
        
        /// <summary>
        /// Checks if this enemy is currently targeting this character or this character is targeting this enemy.
        /// </summary>
        public static bool IsTargetedOrIsTargeting(this NpcElement character, ICharacter target) {
            return character.GetCurrentTarget() == target || character.IsTargetedBy(target);
        }
        
        /// <summary>
        /// List of enemies that can be targeted by this character.
        /// </summary>
        public static FightingPair.LeftStorage GetPossibleTargets(this ICharacter target) {
            return target.PossibleTargets;
        }
        
        /// <summary>
        /// List of enemies that can target this character (have this character on their possible targets list).
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static FightingPair.RightStorage GetPossibleAttackers(this ICharacter target) {
            return target.PossibleAttackers;
        }
        
        /// <summary>
        /// Checks if there is any character that is targeting Hero and can trigger aggro music.
        /// </summary>
        public static bool AnyPossibleAttackerForHero(this Hero hero) {
            var possibleAttackers = hero.PossibleAttackers;
            if (possibleAttackers.IsEmpty()) {
                return false;
            }

            foreach (var character in possibleAttackers) {
                if (character is not NpcElement npc) {
                    return true;
                }

                if (npc.Template.CanTriggerAggroMusic) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Searches for best target to fight with from list of possible targets.
        /// </summary>
        /// <param name="searchNew">Checks surroundings for new targets and fills list of possible targets with them before sorting them.</param>
        public static ICharacter RecalculateTarget(this NpcElement npc, bool searchNew = false, bool force = false, bool canBeVictorious = true) {
            const int FrameCooldown = 1;
            if (!force) {
                if (npc.DisableTargetRecalculation || !npc.IsAlive || !npc.HasPerception) return null;
                if (npc.RecalculationFrameCooldown >= Time.frameCount) return npc.GetCurrentTarget();
            }

            npc.RecalculationFrameCooldown = Time.frameCount + FrameCooldown;

            if (searchNew) {
                npc.FindMissingTargets();
            }
            var target = npc.GetAndUpdateNewTarget();
            
            if (target == null) {
                npc.NpcAI.ExitCombat(canBeVictorious: canBeVictorious);
            } else {
                npc.NpcAI.FoundTarget();
            }
            
            return target;
        }
        
        /// <summary>
        /// Checks if there is a better fittet target and replaces them. Uses minimum difference in fit for hysteresis.
        /// </summary>
        /// <param name="minimumDifference">What is the needed difference in fit value to qualify new target as better.</param>
        public static bool IsBetterTargetCheck(this NpcElement npc, ICharacter currentTarget, float minimumDifference = NormalFitDifference) {
            currentTarget ??= npc.GetCurrentTarget();
            float fitToBeat = npc.IsStillValidWithNpcChecks(currentTarget) ? npc.TargetFit(currentTarget) + minimumDifference : float.MinValue;
            var bestTarget = npc.GetNewTarget(fitToBeat);
            if (bestTarget != null) {
                npc.TryUpdateTarget(bestTarget);
                return true;
            }
            return false;
        }

        static ICharacter GetNewTarget(this NpcElement npc, float fitToBeat = float.MinValue) {
            return TargetOverrideElement.GetTarget(npc) ?? npc.GetNewTargetInternal(fitToBeat);
        }

        static ICharacter GetAndUpdateNewTarget(this NpcElement npc) {
            var target = npc.GetNewTarget();
            npc.TryUpdateTarget(target);
            return target;
        }

        static void TryUpdateTarget(this NpcElement npc, ICharacter target) {
            var related = npc.RelatedValue(Relations.Targets);
            var relatedTarget = related.Get();
            if (relatedTarget != target) {
                if (relatedTarget != null) {
                    npc.DisableTargetRecalculation = true;
                    related.Detach();
                    npc.DisableTargetRecalculation = false;
                }
                related.ChangeTo(target);
                
                if (target is Hero && npc.ParentModel.TryGetElement<EnemyBaseClass>(out var enemy)) {
                    CombatDirector.AddEnemyToFight(enemy);
                }
            }
        }

        static ICharacter GetNewTargetInternal(this NpcElement npc, float minimumFit) {
            Ranged.Clear();
            Melee.Clear();
            
            foreach (ICharacter targeting in npc.GetPossibleTargets()) {
                if (!npc.IsStillValidWithNpcChecks(targeting)) {
                    npc.RemoveCombatTarget(targeting);
                    continue;
                }
                if (targeting.IsRanged()) {
                    Ranged.Add(targeting);
                } else {
                    Melee.Add(targeting);
                }
            }

            var target = BestTargetFromList(npc, Melee, minimumFit) ?? BestTargetFromList(npc, Ranged, minimumFit) ?? BestTargetFromMissingTargets(npc, minimumFit);
            
            Ranged.Clear();
            Melee.Clear();

            return target;
        }

        static ICharacter BestTargetFromList(NpcElement npc, List<ICharacter> targets, float minimumFit) {
            float bestFit = minimumFit;
            ICharacter bestTarget = null;
            foreach (var target in targets) {
                var fit = TargetFit(npc, target);
                if (fit > bestFit) {
                    bestFit = fit;
                    bestTarget = target;
                }
            }
            return bestTarget;
        }

        static ICharacter BestTargetFromMissingTargets(NpcElement npc, float minimumFit) {
            float bestFit = minimumFit;
            ICharacter bestTarget = null;
            var npcs = World.Services.Get<NpcGrid>().GetNpcsInSphere(npc.Coords, npc.NpcAI.Data.perception.RadarRange);
            foreach (var other in npcs) {
                if (other == npc || !npc.IsPossibleTargetNoRangeCheck(other)) {
                    continue;
                }
                if (!npc.AddCombatTargetInternal(other)) {
                    continue;
                }
                var fit = TargetFit(npc, other);
                if (fit > bestFit) {
                    bestFit = fit;
                    bestTarget = other;
                }
            }
            if (Hero.Current is { IsAlive: true } hero) {
                if (npc.IsPossibleTarget(hero)) {
                    if (!npc.AddCombatTargetInternal(hero)) {
                        return bestTarget;
                    }
                    var fit = TargetFit(npc, hero);
                    if (fit > bestFit) {
                        bestFit = fit;
                        bestTarget = hero;
                    }
                }
            }
            return bestTarget;
        }

        static void FindMissingTargets(this NpcElement npc) {
            var npcs = World.Services.Get<NpcGrid>().GetNpcsInSphere(npc.Coords, npc.NpcAI.Data.perception.RadarRange);
            foreach (NpcElement other in npcs) {
                if (other != npc && npc.IsPossibleTargetNoRangeCheck(other)) {
                    npc.AddCombatTargetInternal(other);
                }
            }
            if (Hero.Current is { IsAlive: true } hero) {
                if (npc.IsPossibleTarget(hero)) {
                    npc.AddCombatTargetInternal(hero);
                }
            }
        }
        
        // === TARGET VALIDATIONS
        
        static bool IsPossibleTarget(this NpcElement npc, ICharacter target) {
            return target switch {
                NpcElement targetNpc => IsPossibleTarget(npc, targetNpc),
                Hero hero => IsPossibleTarget(npc, hero),
                ImaginaryTarget => true,
                _ => false
            };
        }
        
        static bool IsPossibleTarget(this NpcElement npc, Hero hero) {
            return hero.IsAlive && npc.WantToFight(hero) 
                                && IsHeroVisible(hero, npc)
                                && hero.Coords.SquaredDistanceTo(npc.Coords) < npc.NpcAI.Data.perception.RadarRangeSq
                                && (AstarPath.active != null && (npc.RequiresPathToTarget == false || PathPossibleCondition(npc, AstarPath.active.GetNearest(npc.Coords).node, hero.ClosestPointOnNavmesh.node)));
        }
        
        static bool IsPossibleTargetNoRangeCheck(NpcElement npc, Hero hero) {
            return hero.IsAlive && npc.WantToFight(hero) 
                                && IsHeroVisible(hero, npc)
                                && (AstarPath.active != null && (npc.RequiresPathToTarget == false || PathPossibleCondition(npc, AstarPath.active.GetNearest(npc.Coords).node, hero.ClosestPointOnNavmesh.node)));
        }
        
        static bool IsPossibleTarget(NpcElement npc, NpcElement targetNpc) {
            return !targetNpc.HasElement<Invisibility>() && npc.WantToFight(targetNpc) 
                                                         && targetNpc is {HasPerception: true, IsAlive: true, IsUnconscious: false}
                                                         && targetNpc.Coords.SquaredDistanceTo(npc.Coords) < npc.NpcAI.Data.perception.RadarRangeSq
                                                         && (AstarPath.active != null && PathPossibleCondition(npc, npc.Coords, targetNpc.Coords));
        }
        
        static bool IsPossibleTargetNoRangeCheck(this NpcElement npc, NpcElement targetNpc) {
            return !targetNpc.HasElement<Invisibility>() && npc.WantToFight(targetNpc)
                                                      && targetNpc is { HasPerception: true, IsAlive: true, IsUnconscious: false }
                                                      && (AstarPath.active != null && PathPossibleCondition(npc, npc.Coords, targetNpc.Coords));
        }
        
        static bool IsStillValidWithNpcChecks(this NpcElement npc, ICharacter target) {
            if (npc is not {NpcAI: {Working: true}}) {
                return false;
            }
            return target switch {
                NpcElement targetNpc => IsStillValid(npc, targetNpc),
                Hero hero => IsStillValid(npc, hero),
                ImaginaryTarget => true,
                _ => false
            };
        }

        static bool IsStillValid(this NpcElement npc, ICharacter target) {
            return target switch {
                NpcElement targetNpc => IsStillValid(npc, targetNpc),
                Hero hero            => IsStillValid(npc, hero),
                ImaginaryTarget      => true,
                _                    => false
            };
        }
        
        static bool IsStillValid(NpcElement npc, NpcElement targetNpc) {
            return npc.WantToFight(targetNpc) && targetNpc is {HasPerception: true, IsAlive: true, IsUnconscious: false, NpcAI: {Working: true} } 
                                              && targetNpc.Coords.SquaredDistanceTo(npc.Coords) < ForceEndCombatDistanceSqr
                                              && (AstarPath.active == null || PathPossibleCondition(npc, npc.Coords, targetNpc.Coords));
        }
        
        static bool IsStillValid(NpcElement npc, Hero hero) {
            return npc.WantToFight(hero) && hero.IsAlive
                                         && hero.Coords.SquaredDistanceTo(npc.Coords) < ForceEndCombatDistanceSqr
                                         && (!npc.NpcAI.CanLoseTargetBasedOnVisibility || npc.NpcAI.HeroVisible || hero.PossibleAttackers.Any(i => i is NpcElement { NpcAI: { HeroVisible: true } }));
            // TODO: This condition should have some sort of delay to don't stop fights immediately after hero is out of navmesh.
            //&& (AstarPath.active == null || PathPossibleCondition(npc, npc.Coords, hero.Coords));
        }

        /// === FIT

        /// <summary>
        /// Checks if new target is a better fittet than old target. Uses minimum difference in fit for hysteresis.
        /// </summary>
        /// <param name="minimumDifference">What is the needed difference in fit value to qualify new target as better.</param>
        public static bool IsBetterFitThanTarget(this NpcElement npc, ICharacter oldTarget, ICharacter newTarget, float minimumDifference = NormalFitDifference) {
            return npc.TargetFit(newTarget) - npc.TargetFit(oldTarget) > minimumDifference;
        }

        /// <summary>
        /// Calculates the fit value of target.
        /// </summary>
        public static float TargetFit(this NpcElement npc, ICharacter target) {
            const float BaseHp = 100f;
            const float BaseDistanceSqr = 5f * 5f;
            const float MaxHpMod = 4f;
            const float MaxDistanceMod = 2f;
            const float MaxTargetingListBonus = 4f;
            const float MaxIsTargetingUsBonus = 2f;
            const float MaxFit = MaxHpMod + MaxDistanceMod + MaxTargetingListBonus + MaxIsTargetingUsBonus;

            if (target is not { IsAlive: true, HasBeenDiscarded: false }) {
                npc.RemoveCombatTarget(target);
                return float.MinValue;
            }
            
            // The easier the target to kill, the better (0 - 4 value)
            float hpMod = BaseHp / target.Stat(AliveStatType.Health);
            hpMod = Mathf.Clamp(hpMod, 0f, MaxHpMod);
            
            // The closer the target, the better (0 - 1 value)
            float distanceMod = BaseDistanceSqr / (npc.Coords - target.Coords).sqrMagnitude;
            distanceMod = Mathf.Clamp(distanceMod, 0f, MaxDistanceMod);
            
            bool isTargetingUs = target is Hero || npc.IsTargetedBy(target);
            bool isTargetedByUs = npc.GetCurrentTarget() == target;
            
            // If someone isn't attacked by anyone we should prioritize them (4 value)
            // The less people are targeting them, the better (-10 - 1 value)
            int targetingAmount = target is NpcElement targetNpc ? targetNpc.GetTargeting().Count() : 0;
            if (isTargetedByUs) {
                targetingAmount--;
            }
            int targetingLimit = target.GetTargetLimit();
            float targetingListBonus;
            if (targetingAmount == 0) {
                targetingListBonus = MaxTargetingListBonus;
            } else if (targetingAmount < targetingLimit) {
                targetingListBonus = (float) (targetingLimit - targetingAmount) / targetingLimit;
            } else {
                // Penalize each targeting over the limit
                targetingListBonus = -MaxFit - MaxFit * (targetingAmount - targetingLimit);
            }
            
            // If someones is attacking us, we should prioritize them (0 - 2 value)
            // It ignores hero
            float isTargetingUsBonus = isTargetingUs ? MaxIsTargetingUsBonus : 0f;

            return hpMod + distanceMod + targetingListBonus + isTargetingUsBonus;
        }

        public static bool IsHeroVisible(Hero hero, NpcElement npc) {
            foreach (var invisibility in hero.Elements<Invisibility>()) {
                if (invisibility.BlocksPerception(npc)) {
                    return false;
                }
            }
            return true;
        }
        
        static int GetTargetLimit(this ICharacter character) {
            return character switch {
                NpcElement npc => npc.CombatSlotsLimit,
                Hero hero => MaxEnemiesPerUnit,
                ImaginaryTarget => 1,
                _ => throw new NotImplementedException(),
            };
        }

        static bool PathPossibleCondition(NpcElement npc, Vector3 from, Vector3 to) {
            if (!npc.RequiresPathToTarget) {
                return true;
            }
            var current = AstarPath.active.GetNearest(from).node;
            var destination = AstarPath.active.GetNearest(to).node;
            return PathPossibleCondition(npc, current, destination);
        }
        
        static bool PathPossibleCondition(NpcElement npc, GraphNode from, GraphNode to) {
            if (!npc.RequiresPathToTarget) {
                return true;
            }
            if (from == null || to == null) {
                return false;
            }
            return PathUtilities.IsPathPossible(from, to);
        }
        
        // === Relations
        public static class Relations {
            static readonly RelationPair<NpcElement, ICharacter> Targeting = new(typeof(Relations), Arity.Many, nameof(Targets), Arity.One,
                nameof(IsTargetedBy), isSaved: false);
            public static readonly Relation<NpcElement, ICharacter> Targets = Targeting.LeftToRight;
            public static readonly Relation<ICharacter, NpcElement> IsTargetedBy = Targeting.RightToLeft;
        }
    }
}
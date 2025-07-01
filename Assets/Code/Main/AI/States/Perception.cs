using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.States {
    [Il2CppEagerStaticClassConstruction]
    public class Perception {
        public const float MinimumHeroSight = 0.2f;
        const int UpdateTargetFrameDelay = 120;
        const int AITargetUpdateSlots = 3;
        const int HeroVisibilityModifierUpdatesPerFrame = 5;
        const int HeroVisibilityModifierUpdateDelay = 3;
        const int CorpsesDetectUpdateSlots = 2;
        const float HeroVisibilityGainSpeedMultiplier = 2f;
        const float HeroVisibilityLoseSpeedMultiplier = 1f;
        const float HeroVisibilityToRadarThreshold = 0.2f;
        const float RetrySetHeroVisibilityDelay = 0.5f;

        public static bool debugMode;
        public static bool debugVisionMode = true;
        public static bool debugNoiseMode = true;
        public static bool debugTargetMode = true;
        static int s_heroVisibilityUpdatesCounter;
        static int s_nextAITargetUpdateSlot;
        static int s_nextCorpsesDetectUpdateSlot;

        public static bool DebugMode => SafeEditorPrefs.GetBool("debug.perception") || debugMode;
        [UnityEngine.Scripting.Preserve] public static bool DebugVisionCones => DebugMode && debugVisionMode;
        public static bool DebugHearingMarkers => DebugMode && debugNoiseMode;
        [UnityEngine.Scripting.Preserve] public static bool DebugTargetMarker => DebugMode && debugTargetMode;

        readonly NpcElement _npc;
        readonly PerceptionData _perception;
        readonly NpcAI _ai;
        
        readonly int _aiTargetUpdateSlot;
        readonly int _corpsesDetectUpdateSlot;
        int _nextHeroVisibilityUpdateFrame;
        
        NpcDangerTracker _dangerTracker;

        float _heroVisibleCounter;
        float _lastHeroVisibilityModifier;
        float _retryUpdateHeroVisibility = RetrySetHeroVisibilityDelay;
        PerceptionDebug _debug;
        NpcCrimeReactions _cachedCrimeReactions;

        float Sight => _npc.NpcStats.Sight;
        float SightLengthMultiplier => _npc.NpcStats.SightLengthMultiplier;
        public bool InAnyDanger => _dangerTracker.InAnyDanger;
        public bool InDirectDanger => _dangerTracker.InDirectDanger;
        public bool InEnviroDanger => _dangerTracker.InEnviroDanger;
        public bool AreFearfulsInDanger => _dangerTracker.FearfulsInDanger;
        public bool ShouldFlee => InDirectDanger || (InEnviroDanger && !_npc.IgnoresEnviroDanger);
        public NpcDangerTracker DangerTracker => _dangerTracker;
        
        public Perception(NpcAI npcAI, NpcData data, bool isFleeing) {
            _aiTargetUpdateSlot = s_nextAITargetUpdateSlot;
            s_nextAITargetUpdateSlot = (s_nextAITargetUpdateSlot + 1) % AITargetUpdateSlots;
            _corpsesDetectUpdateSlot = s_nextCorpsesDetectUpdateSlot;
            s_nextCorpsesDetectUpdateSlot = (s_nextCorpsesDetectUpdateSlot + 1) % CorpsesDetectUpdateSlots;

            _ai = npcAI;
            _npc = _ai.ParentModel;
            _perception = data.perception;
            
            if (isFleeing) {
                _dangerTracker = new NpcDangerTracker(_npc);
            }
        }

        public static void NextFrame() {
            s_heroVisibilityUpdatesCounter = math.max(s_heroVisibilityUpdatesCounter - HeroVisibilityModifierUpdatesPerFrame, 0);
        }

        public void OnStart(bool fleeing) {
            if (fleeing) {
                _dangerTracker.OnStart();
            }
        }

        public void OnStop(bool fleeing, bool discarded) {
            if (fleeing) {
                _dangerTracker.OnStop();
            }
            if (!discarded) { 
                _npc.ForceEndCombat();
            }
            ClearDebug();
        }

        public void UpdateCombatant(float deltaTime) {
            var frameCount = Time.frameCount;
            _ai.AlertStack.Update(deltaTime);

            if (_npc.HasElement<Invisibility>()) return;

            var hero = Hero.Current;
            CalculateHeroVisibility(hero, deltaTime, frameCount, out float heroVisibility);

            bool? wantsToFightHero = null;
            bool WantsToFightHero() => wantsToFightHero ??= _npc.WantToFight(hero);

            var heroFullyVisible = _ai.HeroVisible && heroVisibility > 0 && WantsToFightHero();
            if (heroFullyVisible) {
                float alertStrength = heroVisibility * deltaTime;
                alertStrength *= _ai.InAlert ? _ai.AlertStack.AlertVisionGain : (int) AlertStack.AlertStrength.Strong;
                alertStrength *= _ai.Data.alert.VisionAlertGainMultiplier;
                _ai.AlertStack.NewPoi(alertStrength, hero);
            }

            DetectCorpses(frameCount);

            if (heroVisibility > HeroVisibilityToRadarThreshold && _ai.InAlert &&
                (_npc.Coords - hero.Coords).sqrMagnitude < _perception.HeroRadarRangeSq && WantsToFightHero()) {
                _ai.HeroVisibility = 1f;
                _ai.AlertStack.NewPoi((int) AlertStack.AlertStrength.Max * deltaTime, hero);
            } else if (heroFullyVisible && _ai.GetDistanceToLastIdlePointBand() < 2 && WantsToFightHero()) {
                _ai.HeroVisibility += heroVisibility * HeroVisibilityGainSpeedMultiplier * deltaTime;
            } else if (!heroFullyVisible && _ai.HeroVisibility > 0) {
                _ai.HeroVisibility -= deltaTime *
                                      (_ai.InCombat ? _ai.CombatAggroDecreaseModifierByDistanceToLastIdlePoint() : HeroVisibilityLoseSpeedMultiplier);
            }

            if (!_ai.InCombat) {
                UpdateAITarget(frameCount);
            } else if (_npc.AutoRecalculationFrameCooldown < frameCount) {
                _npc.AutoRecalculationFrameCooldown = frameCount + UpdateTargetFrameDelay;
                var currentTarget = _npc.ForceGetCurrentTarget(out bool validTarget);
                if (currentTarget == hero && (!_ai.HeroVisible || !validTarget && hero.IsAlive)) {
                    _npc.RecalculateTarget(true, canBeVictorious: false);
                } else if (currentTarget == null || !validTarget) {
                    _npc.RecalculateTarget(true);
                } else {
                    _npc.IsBetterTargetCheck(currentTarget, AITargetingUtils.BigFitDifference);
                }
            }
            UpdateDebug(hero);

            _retryUpdateHeroVisibility -= deltaTime;
            if (!_ai.InCombat && heroFullyVisible && _retryUpdateHeroVisibility <= 0) {
                _ai.UpdateHeroVisibility(1);
                _retryUpdateHeroVisibility = RetrySetHeroVisibilityDelay;
            } else if (heroVisibility < 1) {
                _retryUpdateHeroVisibility = RetrySetHeroVisibilityDelay;
            }
        }   

        public void UpdateFleeing(float deltaTime) {
            var frameCount = Time.frameCount;
            var hero = Hero.Current;

            CalculateHeroVisibility(hero, deltaTime, frameCount, out _);
            if (_ai.HeroVisibility > 0) {
                _ai.HeroVisibility -= deltaTime;
            }
            var chunkData = _npc.NpcChunk?.Data;
            bool danger = chunkData?.HasDanger ?? false;
            bool dangerForFearfuls = chunkData?.HasDangerForFearfuls ?? false;
            _dangerTracker.Update(danger, dangerForFearfuls, deltaTime, _npc);
            UpdateDebug(hero);
        }

        void CalculateHeroVisibility(Hero hero, float deltaTime, int frameCount, out float heroVisibility) {
            heroVisibility = HeroDetectionBySight(deltaTime, frameCount, hero, out bool coyoteTimeActive);
            _npc.CachedElement(ref _cachedCrimeReactions).SetSeeingHero(!coyoteTimeActive && heroVisibility >= MinimumHeroSight);
        }

        void UpdateAITarget(int frameCount) {
            if (frameCount % AITargetUpdateSlots != _aiTargetUpdateSlot) {
                return;
            }
            NpcElement aiTarget = null;
            float aiDistanceSq = 0;
            var npcs = World.Services.Get<NpcGrid>().GetNpcsInSphere(_npc.Coords, _perception.RadarRange);
            foreach (var npc in npcs) {
                if (npc == _npc) {
                    continue;
                }
                
                float newAiDistanceSq = _npc.DistanceSqTo(npc);
                bool visible = newAiDistanceSq < _perception.RadarRangeSq && !npc.HasElement<Invisibility>();
                if (visible && _npc.TryAddPossibleCombatTarget(npc) && (newAiDistanceSq < aiDistanceSq || aiTarget == null)) {
                    aiTarget = npc;
                    aiDistanceSq = newAiDistanceSq;
                }
            }

            if (aiTarget == null && !_ai.InCombat) {
                var currentTarget = _ai.NpcElement.GetCurrentTarget();
                if (currentTarget is NpcElement npcElement) {
                    aiTarget = npcElement;
                }
            }

            if (aiTarget is { IsAlive: true, IsUnconscious: false }) {
                _ai.EnterCombatWith(aiTarget, true);
            }
        }

        void DetectCorpses(int frameCount) {
            if (frameCount % CorpsesDetectUpdateSlots != _corpsesDetectUpdateSlot) {
                return;
            }
            var distance = _perception.MaxDistance(_ai);
            _perception.ViewConeSinCos(_ai, out var sin, out var cos);
            var corpses = World.Services.Get<NpcGrid>()
                .GetCorpsesInCone(_npc.Coords, _npc.Forward(), distance, cos, sin);

            foreach (var corpse in corpses) {
                if (corpse.Faction.AntagonismTo(_npc.Faction) != Antagonism.Friendly) {
                    continue;
                }
                if (!corpse.TryToMarkAsViewed(_ai)) {
                    continue;
                }
                _npc.TryGetElement<BarkElement>()?.OnCorpseFound();
                
                var alertRange = World.Services.Get<GameConstants>().CorpsesAlertStrength;
                var point = _ai.Coords;
                foreach (var npc in World.Services.Get<NpcGrid>().GetHearingNpcs(_npc.Coords, _perception.RadarRange)) {
                    var ai = npc.NpcAI;
                    if (ai.TryCalculateAlertStrength(alertRange, NoiseStrength.VeryStrong, 1f, true, point, out float alertStrength, out _)) {
                        NotifyOtherAboutCorpses(corpse, ai, alertStrength);
                    }
                }
            }
            
            static void NotifyOtherAboutCorpses(Corpse corpse, NpcAI other, float alertStrength) {
                if (corpse.TryToMarkAsViewed(other)) {
                    other.AlertStack.NewPoi(alertStrength, corpse.Coords);
                }
            }
        }

        // === Hero detection by sight
        float HeroDetectionBySight(float deltaTime, int frameCount, Hero hero, out bool coyoteTimeActive) {
            if (!AITargetingUtils.IsHeroVisible(hero, _npc)) {
                _ai.HeroVisible = false;
                coyoteTimeActive = true;
                return 0;
            }
            float heroVisibility = CalculatedVisibility(hero, frameCount);
            heroVisibility -= _ai.MinHeroVisibilityByDistanceToLastIdlePoint();
            heroVisibility *= CoyoteTimeHeroVisibilityMultiplier(_ai.HeroVisible, heroVisibility, deltaTime, out coyoteTimeActive);
            heroVisibility = math.max(0, heroVisibility);
            if (_ai.HeroVisible) {
                _ai.MaxHeroVisibilityGain = math.max(_ai.MaxHeroVisibilityGain, heroVisibility);
            } else {
                _ai.MaxHeroVisibilityGain = 0;
            }
            _ai.HeroVisible = hero.IsAlive && heroVisibility > 0;
            return heroVisibility;
        }

        float CalculatedVisibility(Hero hero, int frameCount) {
            var sight = Sight;
            var sightLengthMultiplier = SightLengthMultiplier;
            if (sight <= 0 || sightLengthMultiplier <= 0) {
                return 0;
            }
            var direction = hero.Coords - _npc.Coords;
            var distance = direction.magnitude;
            if (distance >= _perception.MaxDistance(_ai) * sightLengthMultiplier) {
                return 0;
            }

            float result = CalculateVisibilityFactor(direction, distance, sightLengthMultiplier);
            bool maxVisibility = result >= 1.5f;
            if (maxVisibility) {
                result = 1.5f;
            } else if (result < 0f) {
                result = 0f;
            }
            
            float multiplier = hero.IsCrouching ? hero.HeroStats.CrouchVisibilityMultiplier : hero.HeroStats.VisibilityMultiplier;
            result = result * sight * multiplier;
            
            if (maxVisibility && result <= MinimumHeroSight) {
                result = MinimumHeroSight;
            } else if (result <= 0.00001f) {
                return 0;
            }

            if (_nextHeroVisibilityUpdateFrame == frameCount) {
                var visibilityState = _ai.CanSee(hero, _ai.InCombat);
                _lastHeroVisibilityModifier = visibilityState.VisibilityMultiplier;
                _nextHeroVisibilityUpdateFrame += HeroVisibilityModifierUpdateDelay + s_heroVisibilityUpdatesCounter++ / HeroVisibilityModifierUpdatesPerFrame;
            } else if (_nextHeroVisibilityUpdateFrame < frameCount) {
                _nextHeroVisibilityUpdateFrame = frameCount + 1 + s_heroVisibilityUpdatesCounter++ / HeroVisibilityModifierUpdatesPerFrame;
            }
            return result * _lastHeroVisibilityModifier;
        }

        public float CalculateVisibilityFactor(Vector3 direction, float distance, float visionDistanceMultiplier = 1f) {
            var forward = GetLookForward(out var visionRangeMultiplier).ToHorizontal2();
            var dotProduct = Vector2.Dot(direction.ToHorizontal2().normalized, forward);
            return CalculateVisibilityFactor(dotProduct, distance, visionRangeMultiplier * visionDistanceMultiplier);
        }

        public float CalculateVisibilityFactor(float dotProduct, float distance, float visionDistanceMultiplier = 1f) {
            const float CenterCoreDotThreshold = 0.866f;
            const float MinimumOuterDot = 0.4f;
            const float MinimumOuterDotSquared = MinimumOuterDot * MinimumOuterDot;
            
            float coreDistance = _perception.CoreDistance(_ai) * visionDistanceMultiplier;
            float maxDistance = _perception.MaxDistance(_ai) * visionDistanceMultiplier;
            
            // How close target is from AI (not clamped)
            // at coreDistance => 0;
            // at maxDistance => -1;
            float distanceFactor = (coreDistance - distance) / (maxDistance - coreDistance);
            float result;
            if (_perception.IgnoreAngleFactor) {
                float maxDot = _perception.MaxDot(_ai);
                if (dotProduct > maxDot) {
                    result = 1 + distanceFactor;
                } else {
                    return 0f;
                }
            } else {
                float angleConst = _perception.AngleFactor(_ai);
                // How far on the side target is from AI (not clamped)
                // at forward => 1
                // at angle given by Perception.AngleFactor (see Perception.CalculateAngleFactor) => 0

                float angleFactor = (dotProduct + angleConst) / (1 + angleConst);
                result = angleFactor + distanceFactor;
                // Divide vision into Core and Outer. Outer is reduced by squared Dot Product to reduce gain.
                if (result > _perception.VisionCutoff && dotProduct < CenterCoreDotThreshold) {
                    result *= dotProduct > MinimumOuterDot ? dotProduct * dotProduct : MinimumOuterDotSquared;
                    return result;
                }
            }
            
            if (result < _perception.VisionCutoff) {
                result = 0f;
            }
            return result;
        }
        
        public Vector3 GetLookForward(out float visionRangeMultiplier) {
            return GetLookForward(_npc, out visionRangeMultiplier);
        }
        
        public static Vector3 GetLookForward(NpcElement npc, out float visionRangeMultiplier) {
            // Vertical
            const float DotDownThreshold = 0.8f;
            const float DotDownThresholdInversed = 1f / (1 - DotDownThreshold);
            const float MinimumVisionRangeMult = 0.2f;
            const float MinimumVisionRangeMultInAlert = 0.66f;
            // Horizontal
            const float HeadToRootRatio = 0.66f; // the higher, the more vision cone is aligned with head

            var rootForward = npc.Hips.parent.forward;
            var forward = npc.Head.forward;
            var dotDown = Vector3.Dot(forward, Vector3.down);
            forward = forward.ToHorizontal3();
            if (dotDown is <= DotDownThreshold and >= -DotDownThreshold) {
                visionRangeMultiplier = 1f;
                forward = Vector3.Lerp(rootForward, forward, HeadToRootRatio);
            } else {
                // Steep Angles give unlogical results, they need to be custom handled
                float downLerp = (1 - Mathf.Abs(dotDown)) * DotDownThresholdInversed; // DotThreshold to 1 => 1 to 0
                visionRangeMultiplier = Mathf.Lerp(MinimumVisionRangeMult, 1f, downLerp);
                forward = Vector3.Lerp(rootForward, forward, visionRangeMultiplier * HeadToRootRatio);
                if (npc.NpcAI.InCombat) {
                    visionRangeMultiplier = 1f;
                } else if (npc.NpcAI.InAlert) {
                    // We need to decrease the penalty in alert because not every alert animation is correctly prepared or exists.
                    // Some movements or idles have head down, so we need to decrease the penalty.
                    if (npc.NpcAI.AlertValue < StateAlert.Idle2LookAtPercentage) {
                        visionRangeMultiplier = Mathf.Lerp(MinimumVisionRangeMultInAlert, 1f, npc.NpcAI.AlertValue / StateAlert.Idle2LookAtPercentage);
                    } else {
                        visionRangeMultiplier = 1f;
                    }
                }
            }

            return forward.ToHorizontal3().normalized;
        }

        /// <summary>
        /// This Coyote Time is the time Hero needs to be visible to be accounted as visible
        /// </summary>
        /// <param name="isStillVisible">If true coyote time won't be cleared</param>
        /// <param name="heroVisible">Value of the visibility</param>
        /// <param name="deltaTime">Delta time in seconds</param>
        /// <param name="isCoyoteTimeActive">Outputs if coyoteTime is active because float value is not determenistic enough</param>
        /// <returns>Multiplier which is 1 if Coyote Time satisfied (pass thru) otherwise 0</returns>
        float CoyoteTimeHeroVisibilityMultiplier(bool isStillVisible, float heroVisible, float deltaTime, out bool isCoyoteTimeActive) {
            const float HeroVisibleThreshold = 0.25f;
            const float CoyoteTimeActiveMultiplier = 0.05f;
            const float CoyoteTimeDecreaseSpeed = 1.5f;
            if (!_ai.InIdle) {
                _heroVisibleCounter = World.Services.Get<GameConstants>().HeroVisibilityCoyoteTime;
            }
            if (isStillVisible) {
                isCoyoteTimeActive = false;
                return 1f;
            }
            switch (heroVisible) {
                case <= 0: {
                    if (_heroVisibleCounter > 0) {
                        _heroVisibleCounter -= deltaTime * CoyoteTimeDecreaseSpeed;
                        if (_heroVisibleCounter < 0) {
                            _heroVisibleCounter = 0f;
                        }
                    }
                    isCoyoteTimeActive = true;
                    return 0f;
                }
                case < HeroVisibleThreshold: // There's a minimum speed of coyote time gain
                    _heroVisibleCounter += deltaTime * HeroVisibleThreshold;
                    break;
                default:
                    _heroVisibleCounter *= deltaTime * heroVisible;
                    break;
            }

            isCoyoteTimeActive = _heroVisibleCounter < World.Services.Get<GameConstants>().HeroVisibilityCoyoteTime;
            return isCoyoteTimeActive ? CoyoteTimeActiveMultiplier : 1f;
        }

        public void OnExit() {
            ClearDebug();
        }

        void UpdateDebug(Hero hero) {
            if (DebugMode) {
                if (_perception.RefreshDebug) {
                    _perception.RefreshDebug = false;
                    _debug?.Regenerate();
                } else if (hero.Coords.SquaredDistanceTo(_ai.Coords) < 3600) {
                    _debug ??= new PerceptionDebug(_ai, this, _perception);
                    _debug.Update();
                } else if (_debug != null) {
                    ClearDebug();
                }
            } else {
                ClearDebug();
            }
        }

        void ClearDebug() {
            _debug?.Clear();
            _debug = null;
        }
    }
}
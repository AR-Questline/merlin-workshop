using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.AI.States.CrimeReactions;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.AI {
    public class DebugAI : IService {
        const string DebugAIKey = "debug.ai.setting.";
        const float MaxDistanceForDebugInfoSqr = 40f * 40f;

        public bool IsEnabled { get; private set; }

        public bool ShowStats {
            get => PlayerPrefs.GetInt(DebugAIKey + "stats", 1) == 1;
            private set => PlayerPrefs.SetInt(DebugAIKey + "stats", value ? 1 : 0);
        }
        public bool ShowState {
            get => PlayerPrefs.GetInt(DebugAIKey + "state", 1) == 1;
            private set => PlayerPrefs.SetInt(DebugAIKey + "state", value ? 1 : 0);
        }
        public bool ShowTargeting {
            get => PlayerPrefs.GetInt(DebugAIKey + "targeting", 1) == 1;
            private set => PlayerPrefs.SetInt(DebugAIKey + "targeting", value ? 1 : 0);
        }
        public bool ShowTurning {
            get => PlayerPrefs.GetInt(DebugAIKey + "turning", 1) == 1;
            private set => PlayerPrefs.SetInt(DebugAIKey + "turning", value ? 1 : 0);
        }
        public bool ShowAnimations {
            get => PlayerPrefs.GetInt(DebugAIKey + "animations", 1) == 1;
            private set => PlayerPrefs.SetInt(DebugAIKey + "animations", value ? 1 : 0);
        }
        public bool ShowAlertAndAggression {
            get => PlayerPrefs.GetInt(DebugAIKey + "alert", 1) == 1;
            private set => PlayerPrefs.SetInt(DebugAIKey + "alert", value ? 1 : 0);
        }
        public bool ShowThievery {
            get => PlayerPrefs.GetInt(DebugAIKey + "thievery", 0) == 1;
            private set => PlayerPrefs.SetInt(DebugAIKey + "thievery", value ? 1 : 0);
        }
        
        static GameObject DebugAIInfoPrefab {
            get {
                if (s_debugAIInfoPrefab == null) {
                    s_debugAIInfoPrefab = Resources.Load<GameObject>("Prefabs/UIComponents/Debug/DebugAI");
                }
                return s_debugAIInfoPrefab;
            }
        }
        static GameObject s_debugAIInfoPrefab;
        
        readonly Dictionary<EnemyBaseClass, Sticker> _debugInfos = new();
        TimeModel _timeModel;

        public void Enable() {
            if (IsEnabled) {
                return;
            }
            _timeModel = World.Add(new TimeModel());
            _timeModel.GetOrCreateTimeDependent()?.WithUpdate(OnUpdate);
            foreach (var enemy in World.All<EnemyBaseClass>()) {
                AddSticker(enemy);
            }
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<EnemyBaseClass>(), this, enemy => AddSticker((EnemyBaseClass)enemy));
            
            IsEnabled = true;
        }

        public void Disable() {
            if (!IsEnabled) {
                return;
            }
            _timeModel?.Discard();
            World.EventSystem.RemoveAllListenersOwnedBy(this);
            _debugInfos.Values.ForEach(s => s.anchor = null);
            _debugInfos.Clear();
            
            IsEnabled = false;
        }

        void AddSticker(EnemyBaseClass enemyBaseClass) {
            Sticker sticker = World.Services.Get<MapStickerUI>().StickTo(enemyBaseClass.ParentModel, new StickerPositioning {
                worldOffset = Vector3.up * 2f,
                screenOffset = new Vector2(25, 0),
                pivot = new Vector2(0, 0)
            });
            Object.Instantiate(DebugAIInfoPrefab, sticker.transform, false);
            enemyBaseClass.ListenTo(Model.Events.BeforeDiscarded, _ => {
                _debugInfos[enemyBaseClass].anchor = null;
                _debugInfos.Remove(enemyBaseClass);
            }, this);
            _debugInfos.Add(enemyBaseClass, sticker);
        }

        void OnUpdate(float deltaTime) {
            Vector3 heroForward = Hero.Current.Rotation * Vector3.forward;

            foreach ((EnemyBaseClass enemy, Sticker debugInfo) in _debugInfos) {
                bool inLogicBand = LocationCullingGroup.InActiveLogicBands(enemy.NpcElement.CurrentDistanceBand);
                if (!inLogicBand) {
                    debugInfo.gameObject.SetActive(false);
                    continue;
                }
                
                if (enemy.NpcElement.NpcAI == null) {
                    debugInfo.gameObject.SetActive(false);
                    continue;
                }
                
                Vector3 directionToHero = debugInfo.anchor.position - Hero.Current.Coords;
                if (!AIUtils.IsInHeroViewCone(Vector3.Dot(heroForward, directionToHero.normalized))
                    || directionToHero.sqrMagnitude > MaxDistanceForDebugInfoSqr) {
                    debugInfo.gameObject.SetActive(false);
                    continue;
                }
                
                debugInfo.gameObject.SetActive(true);
                TextMeshProUGUI text = debugInfo.GetComponentInChildren<TextMeshProUGUI>();
                
                IState currentState = enemy.NpcElement.NpcAI.Behaviour.CurrentState;
                if (currentState is StateAIWorking aiWorking) {
                    currentState = aiWorking.CurrentState;
                }

                IState detailedState = null;
                if (currentState is NpcStateMachine<StateAIWorking> state) {
                    detailedState = state.CurrentState;
                }
                IBehaviourBase combatBehaviour = enemy.CurrentBehaviour.Get();

                StringBuilder sb = new();
                if (ShowStats) {
                    sb.AppendLine($"HP: {enemy.NpcElement.Health.ModifiedInt}/{enemy.NpcElement.MaxHealth.ModifiedInt}");
                    sb.AppendLine($"SP: {enemy.NpcElement.CharacterStats.Stamina.ModifiedInt}/{enemy.NpcElement.CharacterStats.MaxStamina.ModifiedInt}");
                    sb.AppendLine($"Poise: {enemy.NpcElement.NpcStats.PoiseThreshold.ModifiedInt}/{enemy.NpcElement.NpcStats.PoiseThreshold.UpperLimit}");
                    sb.AppendLine($"Force: {enemy.NpcElement.NpcStats.ForceStumbleThreshold.ModifiedInt}/{enemy.NpcElement.NpcStats.ForceStumbleThreshold.UpperLimit}");
                    sb.AppendLine($"Armor: {enemy.NpcElement.AliveStats.Armor.ModifiedInt}");
                }

                if (ShowState) {
                    sb.AppendLine($"State: {currentState.GetType().Name} Detailed: {detailedState?.GetType().Name}");
                    if (currentState is StateCombat) {
                        sb.AppendLine($"Behaviour: {combatBehaviour?.GetType().Name}");
                    }
                    if (currentState is StateIdle) {
                        sb.AppendLine($"Interaction: {enemy.NpcElement.Behaviours.CurrentInteraction}");
                    }
                    if (detailedState is StatePlayerSuspicious statePlayerSuspicious) {
                        if (statePlayerSuspicious.Movement.CurrentState is FollowMovement following) {
                            sb.AppendLine("  FollowMovement: ");
                            sb.AppendLine($"    Velocity: {(following.VelocityScheme == VelocityScheme.Trot ? "Trot" : "Walk")}");
                            sb.AppendLine($"    Wait Time: {following.WaitTimeLeft}".ColoredTextIf(following.WaitTimeLeft != null, ARColor.EditorBlue));
                            sb.AppendLine($"    Distance to start: {following.DistanceFromStart}");
                            
                        } else if (statePlayerSuspicious.Movement.CurrentState is NoMoveAndRotateTowardsCustomTarget wander) {
                            sb.AppendLine($"  RotateTowardsPlayer");
                        }
                    }
                }
                
                if (ShowTargeting) {
                    var npc = enemy.NpcElement;
                    var target = npc.RelatedValue(AITargetingUtils.Relations.Targets)?.Get();
                    if (target != null) {
                        sb.AppendLine($"Distance to current target: {enemy.DistanceToTarget}");
                        sb.AppendLine($"Angle to current target: {Vector3.SignedAngle(npc.Forward(), target.Coords - npc.Coords, Vector3.up)}");
                    }

                    sb.AppendLine($"Target:\n - {GetTargetingName(npc, target)}");
                    var targetedBy = npc.RelatedList(AITargetingUtils.Relations.IsTargetedBy).Select(t => GetTargetingName(npc, t)).ToArray();
                    sb.AppendLine($"Targeted by ({targetedBy.Length} / {npc.CombatSlotsLimit}):\n - {string.Join(" ,", targetedBy)}");
                    var possibleTargets = npc.PossibleTargets.Select(t => GetTargetingName(npc, t)).ToArray();
                    sb.AppendLine($"Possible targets ({possibleTargets.Length}):\n - {string.Join("\n - ", possibleTargets)}");
                }

                if (ShowTurning) {
                    sb.AppendLine($"Turning:");
                    sb.AppendLine($"  Root offset: {enemy.NpcElement.Controller.RootRotationTrackingOffset}");
                    sb.AppendLine($"  Target delta: {enemy.NpcElement.Controller.TargetRotationDelta}");
                    sb.AppendLine($"  Angular speed: {enemy.NpcElement.Controller.AngularSpeed}");
                }

                if (ShowAnimations) {
                    float multiplier = enemy.CharacterStats.MovementSpeedMultiplier;
                    sb.AppendLine($"Velocity:{enemy.NpcAnimancer.MovementSpeed*multiplier:0.00}, X:{enemy.NpcAnimancer.VelocityHorizontal*multiplier:0.00}, Y:{enemy.NpcAnimancer.VelocityForward*multiplier:0.00}");
                    AppendAnimatorState(sb, "General", enemy.NpcElement.Element<NpcGeneralFSM>());
                    AppendAnimatorState(sb, "Additive", enemy.NpcElement.Element<NpcAdditiveFSM>());
                    AppendAnimatorState(sb, "Custom", enemy.NpcElement.Element<NpcCustomActionsFSM>());
                    AppendAnimatorState(sb, "TopBody", enemy.NpcElement.Element<NpcTopBodyFSM>());
                }
                
                if (ShowAlertAndAggression) {
                    sb.AppendLine($"Alert: {enemy.NpcElement.NpcAI.AlertValue}");
                    sb.AppendLine($"Hero Visibility: {enemy.NpcElement.NpcAI.HeroVisibility}");
                    sb.AppendLine($"Max Hero Visibility Gain: {enemy.NpcElement.NpcAI.MaxHeroVisibilityGain}");
                    sb.AppendLine($"Aggression: {enemy.AggressionScore}");
                }

                if (ShowThievery) {
                    var thievery = enemy.NpcElement.Element<NpcCrimeReactions>();
                    if (thievery != null) {
                        sb.AppendLine($"Thievery:".ColoredText(ARColor.MainAccent));
                        sb.AppendLine($"  Seeing Hero: {thievery.IsSeeingHero}".ColoredTextIf(thievery.IsSeeingHero, ARColor.MainGreen));
                        sb.AppendLine($"  Observing: {thievery.ObserveTime}".ColoredTextIf(thievery.ObserveTime > 0, ARColor.EditorBlue));
                        sb.AppendLine($"  Pickpocket Alert: {thievery.PickpocketAlert}".ColoredTextIf(thievery.PickpocketAlert > 0, ARColor.MainRed));
                        sb.AppendLine($"  Pickpocketing: {thievery.HasBeenPickpocketed}".ColoredTextIf(thievery.HasBeenPickpocketed, ARColor.MainRed));
                        sb.AppendLine($"  Lockpick Crime: {thievery.TimeToLockpickCrime}".ColoredTextIf(thievery.TimeToLockpickCrime > 0, ARColor.MainRed));
                    }
                }

                text.text = sb.ToString();

                static void AppendAnimatorState(StringBuilder sb, string name, NpcAnimatorSubstateMachine machine) {
                    sb.AppendLine($"Animator {name}");
                    sb.AppendLine($"  State: {machine.CurrentAnimatorState.Type}");
                    sb.AppendLine($"  CLip: {machine.AnimationClipName}");
                }
            }
        }

        public void ToggleStats() {
            ShowStats = !ShowStats;
        }
        public void ToggleState() {
            ShowState = !ShowState;
        }
        public void ToggleTargeting() {
            ShowTargeting = !ShowTargeting;
        }
        public void ToggleTurning() {
            ShowTurning = !ShowTurning;
        }
        public void ToggleDebugAnimations() {
            ShowAnimations = !ShowAnimations;
        }
        public void ToggleAlertAndAggression() {
            ShowAlertAndAggression = !ShowAlertAndAggression;
        }
        public void ToggleThievery() {
            ShowThievery = !ShowThievery;
        }

        static string GetTargetingName(NpcElement npc, ICharacter target) {
            return target == null ? "" : $"{(target is Hero ? nameof(Hero) : target.Name)} ({npc.TargetFit(target):F3})";
        }

        public static GameObject SpawnDebugObject(Vector3 pos, string name, float lifetime = -1, PrimitiveType type = PrimitiveType.Sphere) {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.transform.position = pos;
            gameObject.name = name;
            Object.Destroy(gameObject.GetComponent<Collider>());
            if (lifetime > 0) {
                Object.Destroy(gameObject, lifetime);
            }
            return gameObject;
        }
    }
}
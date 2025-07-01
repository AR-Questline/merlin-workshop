using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.HUD;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Customs {
    public partial class StagfathersTrial : AbstractLocationAction, IRefreshedByAttachment<StagfathersTrialAttachment>, UnityUpdateProvider.IWithUpdateGeneric, ITrialElement {
        public override ushort TypeForSerialization => SavedModels.StagfathersTrial;

        [Saved] ITrialElement.TrialState _state = ITrialElement.TrialState.Available;
        [Saved] List<WeakModelRef<NpcElement>> _preys;
        StagfathersTrialAttachment _spec;
        
        public float TrialRemainingDuration { get; private set; }
        public float TrialDuration => _spec.trialDuration;
        public string TrialTitle => _spec.TrialTitle;

        public void InitFromAttachment(StagfathersTrialAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() { }

        protected override void OnRestore() {
            if (_state == ITrialElement.TrialState.InProgress) {
                ParentModel.AfterFullyInitialized(FailTrial, this);
            }
        }
        
        public void UnityUpdate() {
            TrialRemainingDuration -= Time.deltaTime;
            if (TrialRemainingDuration < 0) {
                this.Trigger(ITrialElement.Events.TrialTimeUpdate, 0);
                FailTrial();
                return;
            }
            this.Trigger(ITrialElement.Events.TrialTimeUpdate, TrialRemainingDuration);
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            if (_state is ITrialElement.TrialState.Unavailable or ITrialElement.TrialState.InProgress) {
                return ActionAvailability.Disabled;
            }
            return base.GetAvailability(hero, interactable);
        }
        
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            switch (_state) {
                case ITrialElement.TrialState.Available:
                    StartTrial();
                    break;
                case ITrialElement.TrialState.Unavailable:
                    return;
                case ITrialElement.TrialState.InProgress:
                    return;
                case ITrialElement.TrialState.AwaitingReward:
                    ClaimReward();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StartTrial() {
            if (_spec.startBookmark is { IsValid: true }) {
                Story.StartStory(StoryConfig.Location(ParentModel, _spec.startBookmark, typeof(VDialogue)));
            } else {
                Log.Minor?.Error($"{nameof(_spec.startBookmark)} is not set for Stagefather's Trial on {LogUtils.GetDebugName(ParentModel)} at {_spec.gameObject.PathInSceneHierarchy()}");
            }
            
            _state = ITrialElement.TrialState.InProgress;

            SpawnPreys();
            
            TrialRemainingDuration = _spec.trialDuration;
            World.SpawnView<VTrialDurationTracker>(this, true, true);
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);

            if (_spec.startEffectVFX is { IsSet: true }) {
                PrefabPool.InstantiateAndReturn(_spec.startEffectVFX, ParentModel.Coords, ParentModel.Rotation).Forget();
            }
        }

        public void CompleteTrial() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            
            if (_spec.completeBookmark is { IsValid: true }) {
                Story.StartStory(StoryConfig.Location(ParentModel, _spec.completeBookmark, typeof(VDialogue)));
            } else {
                Log.Minor?.Error($"{nameof(_spec.completeBookmark)} is not set for Stagefather's Trial on {LogUtils.GetDebugName(ParentModel)} at {_spec.gameObject.PathInSceneHierarchy()}");
            }

            _state = ITrialElement.TrialState.AwaitingReward;
            this.Trigger(ITrialElement.Events.TrialEnded, true);
        }
        
        public void FailTrial() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            
            if (_spec.failBookmark is { IsValid: true }) {
                Story.StartStory(StoryConfig.Location(ParentModel, _spec.failBookmark, typeof(VDialogue)));
            } else {
                Log.Minor?.Error($"{nameof(_spec.failBookmark)} is not set for Stagefather's Trial on {LogUtils.GetDebugName(ParentModel)} at {_spec.gameObject.PathInSceneHierarchy()}");
            }
            
            _state = ITrialElement.TrialState.Unavailable;

            // Kill remaining preys
            foreach (var preyRef in _preys) {
                if (preyRef.TryGet(out var prey)) {
                    var location = prey.ParentModel;
                    if (location.IsVisualLoaded) {
                        location.Kill();
                    } else {
                        location.OnVisualLoaded(_ => KillAfterFrame(location).Forget());
                    }

                    static async UniTaskVoid KillAfterFrame(Location locationToKill) {
                        if (await AsyncUtil.DelayFrame(locationToKill)) {
                            locationToKill.Kill();
                        }
                    }
                }
            }
            _preys.Clear();

            // Retry Cooldown
            ARDateTime targetTime = World.Only<GameRealTime>().WeatherTime + _spec.retryAfterFailCooldown;
            List<DeferredCondition> conditions = new() {
                new DeferredTimeCondition(targetTime),
                new DeferredLocationExistCondition(ParentModel)
            };
            ITrialElement.TrialReactivateDeferredAction action = new(this, conditions);
            World.Only<DeferredSystem>().RegisterAction(action);
            
            this.Trigger(ITrialElement.Events.TrialEnded, false);
        }

        public void ReactivateTrialAfterFail() {
            _state = ITrialElement.TrialState.Available;
        }

        public void ClaimReward() {
            if (_spec.rewardBookmark is { IsValid: true }) {
                Story.StartStory(StoryConfig.Location(ParentModel, _spec.rewardBookmark, typeof(VDialogue)));
            } else {
                Log.Minor?.Error($"{nameof(_spec.rewardBookmark)} is not set for Stagefather's Trial on {LogUtils.GetDebugName(ParentModel)} at {_spec.gameObject.PathInSceneHierarchy()}");
            }
            
            _state = ITrialElement.TrialState.Unavailable;
            
            if (_spec.rewardEffectVFX is { IsSet: true }) {
                PrefabPool.InstantiateAndReturn(_spec.rewardEffectVFX, ParentModel.Coords, ParentModel.Rotation).Forget();
            }
        }

        void SpawnPreys() {
            _preys = new List<WeakModelRef<NpcElement>>();
            foreach (var spawnPosition in _spec.spawnPositions) {
                var location = _spec.TrialPrey.SpawnLocation(spawnPosition.position, spawnPosition.rotation);
                var npc = location.Element<NpcElement>();
                _preys.Add(new WeakModelRef<NpcElement>(npc));
                npc.ListenTo(IAlive.Events.BeforeDeath, PreyKilled, this);
                
                if (_spec.spawnPreyEffectVFX is { IsSet: true }) {
                    PrefabPool.InstantiateAndReturn(_spec.spawnPreyEffectVFX, spawnPosition.position, spawnPosition.rotation).Forget();
                }
            }
        }
        
        void PreyKilled(DamageOutcome outcome) {
            if (_state is not ITrialElement.TrialState.InProgress) {
                return;
            }
            if (outcome.Target is not NpcElement npc) {
                return;
            }
            _preys.Remove(new WeakModelRef<NpcElement>(npc));
            if (_preys.IsEmpty()) {
                CompleteTrial();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
        }
    }
}
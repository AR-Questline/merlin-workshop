using System;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using static Awaken.TG.Main.Locations.Actions.Attachments.LogicEmitterOnEventAttachment;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public partial class LogicEmitterOnEvent : Element<Location>, IRefreshedByAttachment<LogicEmitterOnEventAttachment> {
        public override ushort TypeForSerialization => SavedModels.LogicEmitterOnEvent;

        LogicEmitterOnEventAttachment _spec;

        public void InitFromAttachment(LogicEmitterOnEventAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            ParentModel.AfterFullyInitialized(AttachEvents);
        }

        void AttachEvents() {
            if (ParentModel.TryGetElement<NpcElement>() is not {HasBeenDiscarded: false} npc) {
                return;
            }
            
            if (_spec.separateEvents) {
                AttachEvents(this, npc, _spec.eventToTriggerFrom, () => OnSeparateEventTrigger(true));
                AttachEvents(this, npc, _spec.eventToDisableFrom, () => OnSeparateEventTrigger(false));
            } else {
                AttachEvents(this, npc, _spec.eventToTriggerFrom, OnEventTrigger);
            }
        }

        static void AttachEvents(Model owner, NpcElement npc, LogicEmitterEvent eventType, Action onEventTrigger) {
            if (eventType == LogicEmitterEvent.SeesHero) {
                npc.NpcAI.ListenTo(NpcAI.Events.HeroSeenChanged, newState => {
                    if (newState) {
                        onEventTrigger.Invoke();
                    }
                }, owner);
            } else if (eventType == LogicEmitterEvent.StoppedSeeingHero) {
                npc.NpcAI.ListenTo(NpcAI.Events.HeroSeenChanged, newState => {
                    if (!newState) {
                        onEventTrigger.Invoke();
                    }
                }, owner);
            } else if (eventType == LogicEmitterEvent.Damaged) {
                npc.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, onEventTrigger.Invoke, owner);
            } else if (eventType == LogicEmitterEvent.Killed) {
                npc.ListenTo(IAlive.Events.AfterDeath, onEventTrigger.Invoke, owner);
            } else if (eventType == LogicEmitterEvent.EntersCombat) {
                npc.ListenTo(NpcAI.Events.NpcStateChanged, change => {
                    if (change is (_, StateCombat)) {
                        onEventTrigger.Invoke();
                    }
                }, owner);
            } else if (eventType == LogicEmitterEvent.ExitsCombat) {
                npc.ListenTo(NpcAI.Events.NpcStateChanged, change => {
                    if (change is (StateCombat, _)) {
                        onEventTrigger.Invoke();
                    }
                }, owner);
            } else if (eventType == LogicEmitterEvent.EntersAlert) {
                npc.ListenTo(NpcAI.Events.NpcStateChanged, change => {
                    if (change is (_, StateAlert)) {
                        onEventTrigger.Invoke();
                    }
                }, owner);
            } else if (eventType == LogicEmitterEvent.ExitsAlert) {
                npc.ListenTo(NpcAI.Events.NpcStateChanged, change => {
                    if (change is (StateAlert, _)) {
                        onEventTrigger.Invoke();
                    }
                }, owner);
            } else if (eventType == LogicEmitterEvent.None) {
                Log.Minor?.Warning("No event specified for LogicEmitterOnEvent");
            } else {
                Log.Important?.Error($"'{eventType}' Event type not supported for: '{LogUtils.GetDebugName(owner)}'");
            }
        }

        void OnEventTrigger() {
            foreach (Location matchingLocation in _spec.Locations) {
                if (matchingLocation.TryGetElement<LogicReceiverAction>() is not { } receiver) {
                    receiver = matchingLocation.AddElement<LogicReceiverAction>();
                }
                if (_spec.targetState == EmitLogicTargetState.Enabled)
                    receiver.OnActivation(true);
                else if (_spec.targetState == EmitLogicTargetState.Disabled)
                    receiver.OnActivation(false);
                else if (_spec.targetState == EmitLogicTargetState.Toggle) 
                    receiver.OnActivation(!receiver.IsActive);
            }
            if (_spec.once) {
                Discard();
            }
        }

        void OnSeparateEventTrigger(bool targetValue) {
            foreach (Location matchingLocation in _spec.Locations) {
                if (matchingLocation.TryGetElement<LogicReceiverAction>() is not { } receiver) {
                    receiver = matchingLocation.AddElement<LogicReceiverAction>();
                }
                receiver.OnActivation(targetValue);
            }
        }
    }

    public enum LogicEmitterEvent : byte {
        None,
        SeesHero,
        StoppedSeeingHero,
        Damaged,
        Killed,
        EntersCombat,
        ExitsCombat,
        EntersAlert,
        ExitsAlert,
    }
}
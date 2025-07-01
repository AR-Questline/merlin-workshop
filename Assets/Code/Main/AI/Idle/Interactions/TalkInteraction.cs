using System;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class TalkInteraction : INpcInteraction {
        NpcElement _npc;
        NoMove _noMove = new();
        GroundedPosition _lookAt;
        bool _rotateToHeroAtStart;
        
        public event Action OnInternalEnd;
        
        public bool CanBeInterrupted { get; private set; }
        public bool AllowBarks => false;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => false;
        public float? MinAngleToTalk => null;
        public int Priority => 3;
        public bool FullyEntered => true;

        public TalkInteraction(bool rotateToHeroAtStart) {
            _rotateToHeroAtStart = rotateToHeroAtStart;
            CanBeInterrupted = true;
        }

        public Vector3? GetInteractionPosition(NpcElement npc) => null;
        public Vector3 GetInteractionForward(NpcElement npc) => Vector3.zero;

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) => this.BookOneNpc(ref _npc, npc);

        public void Unbook(NpcElement npc) {
            _npc = null;
        }

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            if (NpcPresence.InAbyss(npc.Coords)) {
                Log.Important?.Error($"Talk with someone in abyss! Presence: {npc.NpcPresence}");
            }
            npc.ParentModel.Trigger(StoryInteraction.Events.StoryInteractionToggled, StoryInteractionToggleData.Enter(SpineRotationType.FullRotation));
            npc.Movement.ChangeMainState(_noMove);
            npc.Movement.Controller.ToggleIdleOnlyRichAIActivity(false);
            CanBeInterrupted = false;
            if (_rotateToHeroAtStart) {
                LookAt(npc, GroundedPosition.HeroPosition, false);
            }
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            if (reason == InteractionStopReason.Death) return;
            if (npc is { HasBeenDiscarded: false }) {
                if (npc.Movement != null) {
                    npc.Movement.ResetMainState(_noMove);
                    npc.Movement.Controller.ToggleIdleOnlyRichAIActivity(true);
                }
                npc.ParentModel.Trigger(StoryInteraction.Events.StoryInteractionToggled,
                    StoryInteractionToggleData.Exit(false));
            }
            _lookAt = null;
        }

        public bool IsStopping(NpcElement npc) => false;
        
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) {
            return false;
        }

        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) {
            CanBeInterrupted = true;
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
        }

        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) {
            if (_lookAt != null && _lookAt.Equals(target)) {
                return false;
            }
            
            if (target.IsEqualTo(npc)) {
                Log.Important?.Error($"Npc is trying to look at yourself! {npc}", npc?.ParentModel?.Spec);
                return false;
            }
            
            _lookAt = target;
            npc.ParentModel.Trigger(StoryInteraction.Events.LocationLookAtChanged, new LookAtChangedData(_lookAt, lookAtOnlyWithHead));
            return true;
        }
    }
}
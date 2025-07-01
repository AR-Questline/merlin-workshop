using System;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class StoryCommuteToPosition : INpcInteraction {
        NpcElement _npc;

        readonly Wander _wander;
        readonly Story _storyToInvolveAfterMoved;
        bool _started;

        public bool CanBeInterrupted { get; }
        public bool AllowBarks => true;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => true;
        public float? MinAngleToTalk => null;
        public int Priority => 0;
        public bool FullyEntered => true;

        public event Action OnInternalEnd;

        public StoryCommuteToPosition(VelocityScheme velocityScheme = null, bool waitForEnd = false, Story storyToInvolveAfterMoved = null) {
            _wander = new Wander(CharacterPlace.Default, velocityScheme ?? VelocityScheme.Walk);
            _wander.OnEnd += End;
            CanBeInterrupted = !waitForEnd;
            _storyToInvolveAfterMoved = storyToInvolveAfterMoved;
        }
        
        public void Setup(CharacterPlace characterPlace) {
            _wander.UpdateDestination(characterPlace);
            _wander.UpdateInstantExitRadiusSq(characterPlace.Radius * characterPlace.Radius);
        }

        [UnityEngine.Scripting.Preserve]
        public void Setup(Vector3 position, float positionRange, float exitRadiusSq) {
            _wander.UpdateDestination(new CharacterPlace(position, positionRange));
            _wander.UpdateInstantExitRadiusSq(exitRadiusSq);
        }

        public Vector3? GetInteractionPosition(NpcElement npc) => null;
        Vector3 INpcInteraction.GetInteractionForward(NpcElement npc) => Vector3.zero;

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) => this.BookOneNpc(ref _npc, npc);
        public void Unbook(NpcElement npc) => _npc = null;

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            _started = true;
            if (_npc == null) {
                EndWithError(npc, $"Npc is null. Should by {npc}").Forget();
                return;
            }
            if (_npc.Movement == null) {
                EndWithError(_npc, $"Npc.Movement of {_npc} is null").Forget();
                return;
            }
            if (_npc.Movement.Controller == null) {
                EndWithError(_npc, $"Npc.Movement.Controller of {_npc} is null").Forget();
                return;
            }
            
            if (Vector3.SqrMagnitude(_wander.Destination.Position - _npc.Coords) < _wander.Destination.Radius) {
                End();
                return;
            }
            _npc.Movement.ChangeMainState(_wander);
        }

        async UniTaskVoid EndWithError(NpcElement npc, string error) {
            Log.Important?.Error(error);
            if (await AsyncUtil.DelayFrame(npc, 8) && _started) {
                End();
            }
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            _started = false;
            if (reason == InteractionStopReason.Death) {
                return;
            }
            _npc?.Movement?.ResetMainState(_wander);
        }
        
        public bool IsStopping(NpcElement npc) => false;

        void End() {
            if (_npc == null) {
                return;
            }
            var npc = _npc;
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
            
            if (_storyToInvolveAfterMoved is { HasBeenDiscarded: false }) {
                _storyToInvolveAfterMoved.SetupNpc(npc, true, true, true, true, true).Forget();
            }
        }

        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
    }
}
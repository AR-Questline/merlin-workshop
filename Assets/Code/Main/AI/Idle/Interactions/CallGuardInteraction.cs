using System;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    [UnityEngine.Scripting.Preserve]
    public class CallGuardInteraction : INpcInteraction, UnityUpdateProvider.IWithUpdateGeneric {
        const float DistanceToCallGuard = 10;
        const float DistanceToLookForGuards = 40;
        
        NpcElement _npc;
        NpcElement _guard;

        Wander _wander;

        public bool CanBeInterrupted => false;
        public bool AllowBarks => false;
        public bool AllowDialogueAction => false;
        public bool AllowTalk => false;
        public float? MinAngleToTalk => null;
        public int Priority => 3;
        public bool FullyEntered => true;

        public event Action OnInternalEnd;

        public Vector3? GetInteractionPosition(NpcElement npc) => null;

        public Vector3 GetInteractionForward(NpcElement npc) => Vector3.forward;

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => false;

        public InteractionBookingResult Book(NpcElement npc) => this.BookOneNpc(ref _npc, npc);
        public void Unbook(NpcElement npc) => _npc = null;

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            _wander = new Wander(default, VelocityScheme.Run);
            World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);

            TryFindNewGuard();
        }

        void TryFindNewGuard() {
            _guard = World.Services.Get<NpcGrid>()
                .GetNpcsInSphere(_npc.Coords, DistanceToLookForGuards)
                .Where(npc => npc.Faction == _npc.Faction && npc.IsAlive && CrimeReactionUtils.IsGuard(npc) && !npc.IsInCombat())
                .MinBy(guard => _npc.Coords.SquaredDistanceTo(guard.Coords), true);

            if (_guard == null) {
                End();
                return;
            }

            if (TryCallGuard()) {
                End();
                return;
            }
            
            _wander.UpdateDestination(_guard.Coords, DistanceToCallGuard * 0.5f);
            _npc.Movement.ChangeMainState(_wander);
        }
        
        public void UnityUpdate() {
            if (_guard == null || !_guard.IsAlive || _guard.IsInCombat()) {
                TryFindNewGuard();
                return;
            }
            
            if (TryCallGuard()) {
                End();
                return;
            }
            
            if (_npc.Movement.CurrentState != _wander) {
                _npc.Movement.ChangeMainState(_wander);
            }
            _wander.UpdateDestination(_guard.Coords, DistanceToCallGuard * 0.5f);
        }

        bool TryCallGuard() {
            if (_npc.DistanceSqTo(_guard) < DistanceToCallGuard * DistanceToCallGuard) {
                _guard.NpcAI.EnterCombatWith(Hero.Current);
                return true;
            }
            return false;
        }
        
        void End() {
            World.Services.Get<UnityUpdateProvider>().UnregisterGeneric(this);
            _npc.Movement.ResetMainState(_wander);
            OnInternalEnd?.Invoke();
            OnInternalEnd = null;
        }
        
        public void StopInteraction(NpcElement npc, InteractionStopReason reason) { }
        public bool IsStopping(NpcElement npc) => false;
        
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
    }
}
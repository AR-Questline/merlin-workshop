using System;
using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [Serializable]
    public abstract partial class MistbearerCombatBase : BaseBossCombat {
        NoMoveAndRotateTowardsTarget _noMove;

        [HideInInspector] public bool allSummonsKilled;
        
        public virtual int AmountOfCopies => 0;
        public virtual bool AllCopiesLoaded => true;
        public SummonGroupOfAlliesBehaviour SummonBehaviour => TryGetElement<SummonGroupOfAlliesBehaviour>();

        protected override void AfterVisualLoaded(Transform parentTransform) {
            NpcElement.OnVisualLoaded(AfterVisualLoadedFullyLoaded);
            base.AfterVisualLoaded(parentTransform);
        }

        void AfterVisualLoadedFullyLoaded(NpcElement npc, Transform transform) {
            NpcMovement.ChangeMainState(_noMove = new NoMoveAndRotateTowardsTarget());
        }

        protected override void Tick(float deltaTime, NpcElement npc) {
            var movement = npc.Movement;
            EnforceNoMoveState(Optional<NpcMovement>.NullChecked(movement));
            base.Tick(deltaTime, npc);
        }

        protected override void NotInCombatUpdate(float deltaTime) {
            EnforceNoMoveState(Optional<NpcMovement>.NullChecked(NpcMovement));
            base.NotInCombatUpdate(deltaTime);
        }

        void EnforceNoMoveState(Optional<NpcMovement> movementOptional) {
            if (movementOptional.TryGetValue(out var movement) && movement.CurrentState != _noMove) {
                movement.ChangeMainState(_noMove);
            }
        }

        public virtual void StartTeleportBehaviour() { }

        // Mistbearer Copies
        public virtual void SpawnNewCopies(TeleportDestination[] copyDestinations) { }
        protected override void ApplyForce(Vector3 direction, float forceDamage, float ragdollForce, bool isPush, float duration = 0.5f) { }

        public override void DealPoiseDamage(NpcStateType getHitType, float poiseDamage, bool isCritical, bool isDamageOverTime) {
            // --- Don't enter get hit animations when dying.
            if (NpcElement.IsDying || isDamageOverTime) {
                return;
            }
            SetAnimatorState(NpcStateType.GetHit, NpcFSMType.OverridesFSM);
        }
    }
}
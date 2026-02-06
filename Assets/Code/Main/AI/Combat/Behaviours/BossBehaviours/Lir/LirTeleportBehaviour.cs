using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG {
    [Serializable]
    public class LirTeleportBehaviour : AttackBehaviour<LirCombat> {
        [SerializeField] LocationReference teleportPosition;
        IEventListener _teleportListener;
        
        public override bool CanBeInterrupted => false;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
        public override bool CanBeUsed => Teleports.Any();
        public override bool IsPeaceful => false;
        protected override NpcStateType StateType => NpcStateType.TeleportIn;
        protected override MovementState OverrideMovementState => new NoMove();
        
        IEnumerable<Location> Teleports => teleportPosition.MatchingLocations(null);

        protected override void OnInitialize() {
            ParentModel.ParentModel.AfterFullyInitialized(() => {
                ParentModel.NpcElement.ListenTo(ICharacter.Events.CombatEntered, ApplyCooldown, this);
            });
            base.OnInitialize();
        }

        protected override bool OnStart() {
            _teleportListener = ParentModel.NpcElement?.ListenTo(NpcTeleportState.Events.TeleportAnimationFinished, OnTeleportAnimationFinished, this);
            return true;
        }

        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                Teleport().Forget();
            }
        }

        protected override void OnAnimatorExitDesiredState() {
            // ignore
        }

        protected override void BehaviourExit() {
            World.EventSystem.TryDisposeListener(ref _teleportListener);
            base.BehaviourExit();
        }

        async UniTaskVoid Teleport() {
            ParentModel.NpcElement?.Trigger(ICharacter.Events.SwitchCharacterVisibility, false);
            if (!await AsyncUtil.DelayTime(this, 0.25f)) {
                return;
            }
            
            var target = ParentModel.NpcElement?.GetCurrentTarget();
            var targetPos = target?.Coords ?? ParentModel.Coords;
            var teleportLocation = Teleports.FirstOrDefault();

            if (teleportLocation == null) {
                ParentModel.StopCurrentBehaviour(true);
                Log.Critical?.Error("Missing Teleport Location for Lir Special Attack!");
                return;
            }

            NpcTeleporter.Teleport(ParentModel.NpcElement, new TeleportDestination {
                position = teleportLocation.Coords,
                Rotation = Quaternion.Euler(0, (targetPos - teleportLocation.Coords).ToHorizontal2().Horizontal2ToAngle(), 0)
            }, TeleportContext.FromCombat);
        }

        void AfterTeleported() {
            ParentModel.SetAnimatorState(NpcStateType.TeleportOut);
            ParentModel.NpcElement?.Trigger(ICharacter.Events.SwitchCharacterVisibility, true);
        }

        void OnTeleportAnimationFinished(NpcStateType stateType) {
            if (stateType == NpcStateType.TeleportOut) {
                StartHitScanBehaviour().Forget();
            } else if (stateType == NpcStateType.TeleportIn) {
                AfterTeleported();
            }
        }

        async UniTaskVoid StartHitScanBehaviour() {
            ParentModel.CharacterStats.Stamina.SetToFull();
            if (!await AsyncUtil.DelayTime(this, 0.5f)) {
                return;
            }
            ParentModel.StartBehaviour(ParentModel.TryGetElement<HitScanBehaviour>());
        }
    }
}

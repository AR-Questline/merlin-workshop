using Awaken.Utility;
using System;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcCustomEquipWeapon : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcCustomEquipWeapon;

        NpcStateType _customType = NpcStateType.CustomEquipWeapon;

        public override NpcStateType Type => NpcStateType.CustomEquipWeapon;
        protected override NpcStateType StateToEnter => _customType;

        // === Events
        public new static class Events {
            public static readonly Event<NpcElement, bool> EnteredCustomEquipWeapon = new(nameof(EnteredCustomEquipWeapon));
            public static readonly Event<NpcElement, CustomEquipWeaponType> ChangeCombatExitToCombatState = new(nameof(ChangeCombatExitToCombatState));
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            Npc.ListenTo(Events.ChangeCombatExitToCombatState, OnChangeCombatExitToCombatState, this);
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Npc.Trigger(Events.EnteredCustomEquipWeapon, true);
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
        
        protected override void OnExit(bool restarted) {
            Npc.Trigger(NpcCustomActionsFSM.Events.CustomStateExited, true);
            base.OnExit(restarted);
        }
        
        void OnChangeCombatExitToCombatState(CustomEquipWeaponType exitState) {
            _customType = GetAnimatorStateFromExitState(exitState);
        }
        
        NpcStateType GetAnimatorStateFromExitState(CustomEquipWeaponType exitState) {
            switch (exitState) {
                case CustomEquipWeaponType.Custom:
                    return (Npc.WeaponsHandler?.IsMainHandUsingBackEqSlots() ?? false)
                        ? NpcStateType.CustomEquipWeaponFromBack
                        : NpcStateType.CustomEquipWeapon;
                case CustomEquipWeaponType.Sitting:
                    return NpcStateType.EquipWeaponFromSitting;
                case CustomEquipWeaponType.Crouching:
                    return NpcStateType.EquipWeaponFromCrouching;
                case CustomEquipWeaponType.Lying:
                    return NpcStateType.EquipWeaponFromLying;
                default:
                    throw new ArgumentOutOfRangeException(nameof(exitState), exitState, null);
            }
        }
    }
}
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Alert;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Animations.FSM.Shared;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.Machines {
    public partial class NpcGeneralFSM : NpcAnimatorSubstateMachine, ICanMoveProvider {
        public sealed override bool IsNotSaved => true;

        public override NpcFSMType Type => NpcFSMType.GeneralFSM;
        public bool CanMove => (CurrentAnimatorState?.CanUseMovement ?? true) && !ParentModel.HasElement<SimpleInteractionExitMarker>();
        public bool CanOverrideDestination => CurrentAnimatorState?.CanOverrideDestination ?? true;
        public bool ResetMovementSpeed => CurrentAnimatorState?.ResetMovementSpeed ?? false;
        public override NpcStateType DefaultState => ParentModel.NpcAI.InSpawn ? NpcStateType.Spawn : NpcStateType.Idle;
        public bool IsDyingWithCustomAnimation { get; set; }
        protected override bool EnableOnInitialize => true;

        // === Constructor
        public NpcGeneralFSM(Animator animator, ARNpcAnimancer animancer, int layerIndex, AvatarMask avatarMask) : base(animator, animancer, layerIndex, avatarMask) {
            ModelElements.SetInitCapacity(90);
            ModelElements.SetInitCapacity(typeof(NpcAnimatorState), 75, 0);
            ModelElements.SetInitCapacity(typeof(ARAnimatorState<NpcElement, NpcAnimatorSubstateMachine>), 75, 0);
            ModelElements.SetInitCapacity(typeof(BaseCombatState), 24, 0);
            ModelElements.SetInitCapacity(typeof(AttackGeneric), 1, 10);
        }

        protected override void OnInitialize() {
            AddState(new NpcIdle());
            AddState(new NpcLookAround());
            AddState(new NpcMovement());
            AddState(new NpcShieldedMovement());
            AddState(new NpcTurnMovement());
            AddState(new NpcShieldedTurnMovement());
            AddState(new Attract());
            AddState(new Taunt());
            AddState(new Rest());
            AddState(new DashBack());
            AddState(new DodgeLeft());
            AddState(new DodgeRight());
            AddState(new DodgeBack());
            AddState(new NpcParried());
            AddState(new NpcStaggerEnter());
            AddState(new NpcStaggerLoop());
            AddState(new NpcStaggerExit());
            AddState(new NpcStandUp());
            AddState(new NpcStumbleOneStep());
            AddState(new NpcStumbleThreeStep());
            AddState(new NpcWait());
            AddState(new AttackSpecialAttack());
            AddState(new NpcEquipWeapon());
            AddState(new NpcEquipRangedWeapon());
            AddState(new NpcCustomEquipWeapon());
            AddState(new NpcUnequipWeapon());
            AddState(new PreventDamageStateEnter());
            AddState(new PreventDamageStateLoop());
            AddState(new PreventDamageStateExit());
            AddState(new PreventDamageStateInterrupted());
            AddState(new Fear());
            // --- Melee Attacks
            AddState(new AttackShortRange());
            AddState(new AttackShortRangeCombo());
            AddState(new AttackMediumRange());
            AddState(new AttackLongRange());
            AddState(new AttackLongRangeRunning());
            AddState(new AttackDodge());
            AddState(new NpcChargeEnter());
            AddState(new NpcChargeLoop());
            AddState(new NpcChargeExit());
            AddState(new NpcChargeInterrupted());
            AddState(new AttackGeneric(0));
            AddState(new AttackGeneric(1));
            AddState(new AttackGeneric(2));
            AddState(new AttackGeneric(3));
            AddState(new AttackGeneric(4));
            AddState(new AttackGeneric(5));
            AddState(new AttackGeneric(6));
            AddState(new AttackGeneric(7));
            AddState(new AttackGeneric(8));
            AddState(new AttackGeneric(9));
            // --- Ranged
            AddState(new AttackRangedAttack());
            // --- Magic
            AddState(new AttackMagicAoE());
            AddState(new AttackMagicAura());
            AddState(new AttackMagicProjectile());
            AddState(new AttackMagicRay());
            AddState(new AttackMagicLoopStart());
            AddState(new AttackMagicLoopHold());
            AddState(new AttackMagicLoopEnd());
            // --- Blocking
            AddState(new BlockStart());
            AddState(new BlockHold());
            AddState(new BlockImpact());
            AddState(new BlockMovement());
            // --- GetHits
            AddState(new PoiseBreakFront());
            AddState(new PoiseBreakBackRight());
            AddState(new PoiseBreakBack());
            AddState(new PoiseBreakBackLeft());
            // --- Using Items
            AddState(new UseItemMainHand());
            AddState(new UseItemOffHand());
            // --- Throwing Items
            AddState(new ThrowItemMainHand());
            AddState(new ThrowItemOffHand());
            // --- Wyrdconversion
            AddState(new WyrdConversion(true));
            AddState(new WyrdConversion(false));
            // --- General specials
            AddState(new NpcSpawn());
            AddState(new PhaseTransition(NpcStateType.PhaseTransition));
            AddState(new PhaseTransition(NpcStateType.PhaseTransitionAlternate));
            // --- Alert
            AddState(new AlertStart());
            AddState(new AlertStartQuick());
            AddState(new AlertLookAround());
            AddState(new AlertLookAt());
            AddState(new AlertMovement());
            AddState(new AlertExit());

            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
            NpcCanMoveHandler.AddCanMoveProvider(ParentModel, this);
            
            base.OnInitialize();
        }
        
        void OnDamageTaken(DamageOutcome damageOutcome) {
            NpcStateType hitType = GetPoiseBreakTypeForDamage(damageOutcome);
            bool critical = damageOutcome.DamageModifiersInfo.AnyCritical;
            bool overTime = damageOutcome.Damage.IsDamageOverTime;
            ParentModel.DealPoiseDamage(hitType, damageOutcome.Damage.PoiseDamage, critical, overTime);
        }
        
        NpcStateType GetPoiseBreakTypeForDamage(DamageOutcome damageOutcome) {
            Vector3 damageDirection = damageOutcome.Damage.Direction ??
                                      damageOutcome.Damage.DamageDealer.Forward();

            return GetPoiseBreakTypeForDirection(damageDirection);
        }

        NpcStateType GetPoiseBreakTypeForDirection(Vector3 damageDirection) {
            Vector3 parentForward = ParentModel.Forward();
            
            Vector2 damageDirection2D = damageDirection.ToHorizontal2().normalized;
            Vector2 parentBackDirection2D = parentForward.ToHorizontal2().normalized * -1.0f;

            var angle = Vector2.SignedAngle(damageDirection2D, parentBackDirection2D);
            
            return GetPoiseBreakTypeForAngle(angle);
        }

        NpcStateType GetPoiseBreakTypeForAngle(float deltaAngle) {
            const float StraightBackMaxAngle = 50.0f;
            const float SidewaysBackMaxAngle = 100.0f;
            
            return Mathf.Abs(deltaAngle) switch {
                > SidewaysBackMaxAngle                     => NpcStateType.PoiseBreakFront,
                > StraightBackMaxAngle when deltaAngle < 0 => NpcStateType.PoiseBreakBackRight,
                > StraightBackMaxAngle when deltaAngle > 0 => NpcStateType.PoiseBreakBackLeft,
                _                                          => NpcStateType.PoiseBreakBack
            };
        }

        protected override void OnDisable(bool fromDiscard) {
            if (IsDyingWithCustomAnimation) {
                AnimancerLayer.SetWeight(1);
                return;
            }
            
            base.OnDisable(fromDiscard);
        }
    }
}
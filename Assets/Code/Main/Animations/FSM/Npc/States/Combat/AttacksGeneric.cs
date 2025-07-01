using System;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public sealed partial class AttackGeneric : BaseAttackState {
        public override bool IsNotSaved => true;

        readonly int _index;
        EnemyBaseClass _enemyBaseClass;
        
        public override NpcStateType Type =>
            _index switch {
                0 => NpcStateType.AttackGeneric0,
                1 => NpcStateType.AttackGeneric1,
                2 => NpcStateType.AttackGeneric2,
                3 => NpcStateType.AttackGeneric3,
                4 => NpcStateType.AttackGeneric4,
                5 => NpcStateType.AttackGeneric5,
                6 => NpcStateType.AttackGeneric6,
                7 => NpcStateType.AttackGeneric7,
                8 => NpcStateType.AttackGeneric8,
                9 => NpcStateType.AttackGeneric9,
                _ => throw new ArgumentOutOfRangeException()
            };
        
        public override bool CanBeExited => Data.canBeExited;
        public override bool CanUseMovement => Data.canUseMovement;
        Location Location => Npc.ParentModel;
        EnemyBaseClass EnemyBaseClass => _enemyBaseClass ??= Location.TryGetElement<EnemyBaseClass>();
        GenericAttackData Data => EnemyBaseClass.GenericAttackData;

        public AttackGeneric(int index) {
            _index = index;
        }
        
        public static bool IsGenericAttack(NpcStateType type) {
            return type is >= NpcStateType.AttackGeneric0 and <= NpcStateType.AttackGeneric9;
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (Data.isLooping) {
                return;
            }
            
            if (RemainingDuration <= ParentModel.ExitDurationFromAttackAnimations) {
                ParentModel.SetCurrentState(NpcStateType.Wait, 0.4f);
            }
        }

        public override void Exit(bool restarted = false) {
            EnemyBaseClass.GenericAttackData = GenericAttackData.Default;
            base.Exit(restarted);
        }
    }
}
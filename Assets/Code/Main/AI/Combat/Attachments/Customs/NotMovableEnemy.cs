using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class NotMovableEnemy : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.NotMovableEnemy;

        [SerializeField] bool canRotate = true;
        public override bool CanMove => false;

        NoMove _noMove;
        NoMoveAndRotateTowardsTarget _noMoveAndRotate;
        
        public override void InitFromAttachment(CustomCombatAttachment spec, bool isRestored) {
            NotMovableEnemy copyFrom = (NotMovableEnemy)spec.CustomCombatBaseClass;
            canRotate = copyFrom.canRotate;
            base.InitFromAttachment(spec, isRestored);
        }
        
        protected override void OnInitialize() {
            base.OnInitialize();
            _noMove = new NoMove();
            _noMoveAndRotate = new NoMoveAndRotateTowardsTarget();
        }
        
        protected override void NotInCombatUpdate(float deltaTime) {
            SetMovementState();
            base.NotInCombatUpdate(deltaTime);
        }

        protected override void Tick(float deltaTime, NpcElement npc) {
            SetMovementState();
            base.Tick(deltaTime, npc);
        }

        void SetMovementState() {
            NpcElement.Movement?.InterruptState(canRotate ? _noMoveAndRotate : _noMove);
        }
    }
}
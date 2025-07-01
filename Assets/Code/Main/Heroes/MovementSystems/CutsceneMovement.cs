using Awaken.TG.Graphics.Animations;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class CutsceneMovement : HeroMovementSystem {
        public override ushort TypeForSerialization => SavedModels.CutsceneMovement;

        static readonly int Movement = Animator.StringToHash("Movement");
        
        public override MovementType Type => MovementType.Cutscene;
        public override bool CanCurrentlyBeOverriden => true;
        public override bool RequirementsFulfilled => true;

        public override void Update(float deltaTime) {
            Controller.audioAnimator.SetFloat(Movement, 0);
            Controller.audioAnimator.ResetAllTriggersAndBool();
        }
        public override void FixedUpdate(float deltaTime) { }
        protected override void Init() { }
        protected override void SetupForceExitConditions() { }
    }
}
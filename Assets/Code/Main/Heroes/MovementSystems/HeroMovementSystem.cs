using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using UnityEngine;

//using Unity.VisualScripting;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public enum MovementType : byte {
        [UnityEngine.Scripting.Preserve] Default,
        [UnityEngine.Scripting.Preserve] Mounted,
        [UnityEngine.Scripting.Preserve] Swimming,
        [UnityEngine.Scripting.Preserve] Glider,
        [UnityEngine.Scripting.Preserve] NoClip,
        [UnityEngine.Scripting.Preserve] Teleport,
        [UnityEngine.Scripting.Preserve] Cutscene,
        [UnityEngine.Scripting.Preserve] SnapToPosition,
        [UnityEngine.Scripting.Preserve] DialogueNavmeshBased,
        [UnityEngine.Scripting.Preserve] Finisher,
        [UnityEngine.Scripting.Preserve] HeroKnockdown
    }
    public abstract partial class HeroMovementSystem : Element<Hero> {
        public abstract MovementType Type { get; }
        public abstract bool CanCurrentlyBeOverriden { get; }
        public abstract bool RequirementsFulfilled { get; }
        
        protected VHeroController Controller { get; private set; }
        protected Hero Hero { get; private set; }
        protected HeroStaminaUsedUpEffect StaminaUsedUpEffect { get; private set; }
        public virtual HeroControllerData.HeightData StandingHeight  => Controller.Data.standingHeightData;
        public virtual HeroControllerData.HeightData CrouchingHeight => Controller.Data.crouchingHeightData;
        public float HeadCheckRayLength { get; private set; }

        public void Init(VHeroController controller) {
            Controller = controller;
            Hero = controller.Target;
            StaminaUsedUpEffect = Hero.Element<HeroStaminaUsedUpEffect>();
            HeadCheckRayLength = StandingHeight.height - CrouchingHeight.height;
            Init();
            SetupForceExitConditions();
        }
        public abstract void Update(float deltaTime);
        public abstract void FixedUpdate(float deltaTime);
        public virtual void OnControllerColliderHit(ControllerColliderHit hit) { }
        protected abstract void Init();
        protected abstract void SetupForceExitConditions();

        public new static class Events {
            static readonly OnDemandCache<MovementType, Event<Hero, HeroMovementSystem>> StatChangedByCache = new(st => new($"{nameof(MovementSystemChanged)}/{st.ToStringFast()}"));
            public static Event<Hero, HeroMovementSystem> MovementSystemChanged(MovementType movementType) => StatChangedByCache[movementType];
        }
    }
}
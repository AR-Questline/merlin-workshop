using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class MountedMovement : HeroMovementSystem {
        public override ushort TypeForSerialization => SavedModels.MountedMovement;

        MountElement _mount;
        [UnityEngine.Scripting.Preserve] public VMount MountView { get; private set; }
        public override MovementType Type => MovementType.Mounted;
        public override bool CanCurrentlyBeOverriden => false;
        public override bool RequirementsFulfilled => true;

        protected override void Init() {
            Hero.Trigger(Hero.Events.HideWeapons, true);
            Controller.ToggleCrouch(0, false);
            Controller.UpdateRotationConstraint(true);
        }
        
        public void AssignMount(MountElement mount) {
            _mount = mount;
            MountView = mount.View<VMount>();
        }

        public override void Update(float deltaTime) { }

        public override void FixedUpdate(float deltaTime) { }

        protected override void SetupForceExitConditions() {
            Hero.ListenTo(SCloseHeroEyes.Events.EyesClosed, _ => Dismount(), this);
            Hero.ListenTo(Hero.Events.Revived, _ => Dismount(), this);
            Hero.ListenTo(VJailUI.Events.GoingToJail, _ => Dismount(), this);
        }

        public void Dismount() {
            _mount.Dismount();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Controller.UpdateRotationConstraint(false);
        }
    }
}
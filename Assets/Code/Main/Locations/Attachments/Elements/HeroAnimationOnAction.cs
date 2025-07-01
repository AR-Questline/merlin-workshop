using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class HeroAnimationOnAction : Element<Location>, IRefreshedByAttachment<HeroAnimationOnActionAttachment> {
        public override ushort TypeForSerialization => SavedModels.HeroAnimationOnAction;

        public void InitFromAttachment(HeroAnimationOnActionAttachment spec, bool isRestored) { }

        protected override void OnInitialize() {
            ParentModel.ListenTo(Location.Events.Interacted, PlayAnimation, this);
        }

        void PlayAnimation(LocationInteractionData data) {
            if (data.character is Hero h) {
                h.Trigger(Hero.Events.HideWeapons, true);
                h.Element<HeroOverridesFSM>().SetCurrentState(HeroStateType.HeroCustomInteractionAnimation);
            }
        }
    }
}
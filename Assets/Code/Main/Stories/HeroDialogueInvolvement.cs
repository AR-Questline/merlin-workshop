using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Stories {
    [SpawnsView(typeof(VHeroDialogueInvolvement))]
    public partial class HeroDialogueInvolvement : HeroInvolvement<Story> {
        public override Hero Hero => ParentModel.Hero;
        public override Location FocusedLocation => ParentModel.FocusedLocation;
        public override bool HideHands => false;

        protected override void OnInitialize() {
            Hero.AddElement(new DialogueInvisibility(ParentModel));
            ParentModel.View<VDialogue>()?.ShowFull();
            base.OnInitialize();
            ParentModel.ListenTo(Story.Events.FocusedLocationChanged, TriggerChange, this);
        }
    }
}
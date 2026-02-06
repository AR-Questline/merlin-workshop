using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees {
    public partial class TalentOverviewUI : TalentOverviewBase<VTalentOverviewUI, VTalentTreeTabs> {
        public override bool HasUnsavedChanges => Hero.Talents.AnyUnappliedTalentPoints();
        public override void CreateTabs() {
            AddElement(new TalentTreeTabs());
        }
        public static bool IsViewAvailable() => !World.Services.Get<SceneService>().IsPrologue || Hero.Current.HeroItems.HasItem(CommonReferences.Get.Bonfire.ToRuntimeData(Hero.Current));
    }
}
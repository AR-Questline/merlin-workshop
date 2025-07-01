using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo;
using Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterStats;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Character {
    public partial class CharacterSubTabs : Tabs<CharacterUI, VCharacterTabs, CharacterSubTabType, ICharacterSubTab> {
        protected override KeyBindings Previous => null;
        protected override KeyBindings Next =>  null;
        CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;
        
        protected override void ChangeTab(CharacterSubTabType type) {
            ParentModel.SubTabParent.ToggleTabAndContent(false);
            base.ChangeTab(type);
        }
        
        public void SetNone() {
            ParentModel.SubTabParent.ToggleTabAndContent(true);
            base.ChangeTab(CharacterSubTabType.None);
            RefreshNoneTab().Forget();
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            RefreshNoneTab().Forget();
        }

        async UniTaskVoid RefreshNoneTab() {
            CharacterSheetUI.SetHeroOnRenderVisible(false);
            
            if (await AsyncUtil.DelayFrame(this)) {
                World.Only<Focus>().Select(FirstVisible.button);
            }
        }
    }
    
    public interface ICharacterSubTab : CharacterSubTabs.ITab { }
    public partial class EmptyCharacterSubTab : CharacterSubTabs.TabWithoutView, ICharacterSubTab { }
    
    public abstract partial class CharacterSubTab<TTabView> : CharacterSubTabs.TabWithBackBehaviour<TTabView>, ICharacterSubTab
        where TTabView : View {
        public override void Back() {
            ParentModel.Element<CharacterSubTabs>().SetNone();
        }
    }
    
    public class CharacterSubTabType : CharacterSubTabs.DelegatedTabTypeEnum {
        [UnityEngine.Scripting.Preserve]
        public static readonly CharacterSubTabType
            None = new (nameof(None), string.Empty, _ => new EmptyCharacterSubTab(), Never),
            Overview = new(nameof(Overview), LocTerms.CharacterTabOverview, _ => new CharacterInfoUI(), _ => !World.HasAny<FireplaceUI>()),
            Talents = new(nameof(Talents), LocTerms.CharacterTabTalents, _ => new TalentOverviewUI(), _ => TalentOverviewUI.IsViewAvailable()),
            StatsSummary = new(nameof(StatsSummary), LocTerms.CharacterStatsSummary, _ => new CharacterStatsUI(), Always),
            WyrdArthur = new(nameof(WyrdArthur), LocTerms.CharacterTabWyrdArthur, _ => new WyrdArthurUI(), _ => WyrdArthurUI.IsViewAvailable());
        
        protected CharacterSubTabType(string enumName, string title, SpawnDelegate spawn, VisibleDelegate visible) : base(enumName, title, spawn, visible) { }
    }
}

using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents {
    [UsesPrefab("CharacterSheet/SarrasTalents/" + nameof(VSarrasTalentOverviewUI))]
    public class VSarrasTalentOverviewUI : VTalentOverviewUIBase {
        protected override string RequiredInfoText => LocTerms.UITalentSarrasShrineRequired.Translate();
    } 
}
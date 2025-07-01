using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using TMPro;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroStatTextInfo : ViewComponent<Hero> {
        [RichEnumExtends(typeof(CharacterStatType))]
        public RichEnumReference statType;
        public TextMeshProUGUI statText;

        CharacterStatType StatType => statType.EnumAs<CharacterStatType>();
        LimitedStat LimitedStat => StatType.RetrieveFrom(Target) as LimitedStat;
        
        protected override void OnAttach() {
            Target.ListenTo(Model.Events.AfterChanged, UpdateStatView, Target);
            UpdateStatView();
        }
        
        void UpdateStatView() {
            statText.text = LimitedStat.ToString().FormatSprite();
        }
    }
}
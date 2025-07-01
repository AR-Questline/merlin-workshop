using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;

namespace Awaken.TG.Main.Heroes.CharacterSheet {
    public class VCBaseStatPoints : VCPoints {
        protected override StatType StatType => CharacterStatType.BaseStatPoints;
    }
}
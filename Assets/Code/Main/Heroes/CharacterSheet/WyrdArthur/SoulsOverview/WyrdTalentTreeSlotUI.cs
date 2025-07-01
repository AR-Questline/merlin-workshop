using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur.SoulsOverview {
    public partial class WyrdTalentTreeSlotUI : Element<WyrdArthurPower> {
        public sealed override bool IsNotSaved => true;
        
        public Talent Talent { get; }
        public bool IsUpgraded => Talent is { IsUpgraded: true };

        public WyrdTalentTreeSlotUI(Talent talent) {
            Talent = talent;
        }
    }
}
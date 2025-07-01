using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI {
    public partial class TalentTreeSlotUI : Element<TalentTreeUI> {
        TalentTable CurrentTable => ParentModel.CurrentTable;
        TalentTreeUI TalentTreeUI => ParentModel;
        
        public Talent Talent { get; }
        public bool IsLocked => Talent != null && CurrentTable != null && (Talent.IsLockedByParentTalent || Talent.RequiredTreeLevelToUnlock > CurrentTable.CurrentTreeLevel);
        public bool IsUpgraded => Talent is { IsUpgraded: true };

        public TalentTreeSlotUI(Talent talent) {
            Talent = talent;
        }
        
        public VTalentTreeSlotUI FindTalentParent() {
            return TalentTreeUI.FindTalentSlot(Talent.Parent);
        }
        
        public IEnumerable<VTalentTreeSlotUI> FindTalentChildren() {
            return TalentTreeUI.FindTalentChildren(Talent);
        }
    }
}
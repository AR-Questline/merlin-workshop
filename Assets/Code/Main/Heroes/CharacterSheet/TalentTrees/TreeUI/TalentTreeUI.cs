using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI {
    [SpawnsView(typeof(VTalentTreeUI), false)]
    public partial class TalentTreeUI : Element<TalentOverviewUI>, ITreePatternHost {
        public TalentTable CurrentTable { get; private set; }
        public bool InCategory { get; private set; }
        public Transform TreeParent => View.TreeParent;
        
        TalentTreeSlotUI SelectedSlotUI { get; set; }
        VTalentTreeUI View => View<VTalentTreeUI>();
        
        public void GoToSubTree() {
            InCategory = true;
        }
        
        public void Back() {
            if (InCategory == false) return;

            InCategory = false;
            View.Back();
        }
        
        public void SelectTalent(TalentTreeSlotUI slot, bool state) {
            switch (state) {
                // deselect only if it's the same slot to prevent unintended deselection when hovering over another slot
                case false when SelectedSlotUI == slot:
                    SelectedSlotUI = null;
                    View.HideTooltip();
                    break;
                case true:
                    SelectedSlotUI = slot;
                    View.ShowTooltip(SelectedSlotUI.Talent);
                    break;
            }
            
            ParentModel.RefreshPrompts(SelectedSlotUI?.Talent);
        }
        
        public bool NotLockedByChildren(Talent parent) {
            if (parent == null) return false;
            var children = FindTalentChildren(parent);
            if (children.Any() == false) return true;
            return parent.EstimatedLevel > 1 || children.All(child => !child.Target.Talent.IsUpgraded);
        }
        
        public VTalentTreeSlotUI FindTalentSlot(TalentTemplate parent) {
            return Elements<TalentTreeSlotUI>().FirstOrDefault(slot => slot.Talent.Template == parent)!.View<VTalentTreeSlotUI>();
        }
        
        public VTalentTreeSlotUI[] FindTalentChildren(Talent parent) {
            return Elements<TalentTreeSlotUI>().Where(slot => slot.Talent.Parent == parent.Template).Select(slot => slot.View<VTalentTreeSlotUI>()).ToArray();
        }
        
        public void Fill(TalentTable table) {
            SelectedSlotUI = null;
            CurrentTable = table;
            RemoveElementsOfType<TalentTreeSlotUI>();
            RemoveElementsOfType<TalentTreePatternElement>();
            
            if (CurrentTable.TreeTemplate.Pattern != null) {
                var pattern = AddElement(new TalentTreePatternElement(CurrentTable.TreeTemplate));
                View.SetupPattern(pattern.View<VTalentTreePattern>());
                
                for (int i = 0; i < CurrentTable.talents.Count; i++) {
                    var talentTreeSlot = AddElement(new TalentTreeSlotUI(CurrentTable.talents[i]));
                    View.SpawnSlot(talentTreeSlot, i);
                }
            } else {
                Log.Important?.Error($"No pattern found for talent tree {CurrentTable.TreeTemplate.name}");
            }
            
            View.FocusCurrentSubtree(null).Forget();
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            SelectedSlotUI = null;
        }
    }
}
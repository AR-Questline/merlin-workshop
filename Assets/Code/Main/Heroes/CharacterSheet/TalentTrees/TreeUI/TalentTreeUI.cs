using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern.Host;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI {
    [SpawnsView(typeof(VTalentTreeUI), false)]
    public partial class TalentTreeUI : Element<ITalentOverview>, ITreePatternHost {
        public override bool IsNotSaved => true;
        public TalentTable CurrentTable { get; private set; }
        public bool InCategory { get; private set; }
        public Transform TreeParent => View.TreeParent;
        public SpriteReference LineSpriteReference => View.LineSpriteReference;
        
        TalentTreeSlotUI SelectedSlotUI { get; set; }
        VTalentTreeUI View => View<VTalentTreeUI>();
        VTalentTreePatternHost PatternHost { get; set; }
        
        public new static class Events {
            public static readonly Event<TalentTreeUI, bool> TreeSpawned = new(nameof(TreeSpawned));
            public static readonly Event<TalentTreeUI, bool> TreeZoomedIn = new(nameof(TreeZoomedIn));
        }
        
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
            
            ParentModel.RefreshPromptsActive(SelectedSlotUI?.Talent);
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
            if (PatternHost != null) PatternHost.Discard();

            if (CurrentTable.TreeTemplate.PatternType != null) {
                PatternHost = World.SpawnView(this, CurrentTable.TreeTemplate.PatternType) as VTalentTreePatternHost;
                if (PatternHost != null) View.SetupPattern(PatternHost.Pattern as VTalentTreePattern);

                for (int i = 0; i < CurrentTable.talents.Count; i++) {
                    var talent = CurrentTable.talents[i];
                    var talentTreeSlot = AddElement(new TalentTreeSlotUI(talent));
                    var uiSlot = PatternHost.Pattern.GetSlotForTalent(talent);
            
                    if (uiSlot == null) {
                        Log.Critical?.Error($"No tree node found for talent {talent.Template.name} in pattern {PatternHost.Pattern.name}", View);
                        continue;
                    }
            
                    var slotView = World.SpawnView<VTalentTreeSlotUI>(talentTreeSlot, true, true, uiSlot);
                    View.SetupSlot(slotView, i);
                }
                
                this.Trigger(Events.TreeSpawned, true);
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
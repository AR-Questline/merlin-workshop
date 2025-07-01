using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items.Tooltips.Base;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.WyrdArthur.SoulsOverview {
    [SpawnsView(typeof(VWyrdArthurPower))]
    public partial class WyrdArthurPower : Element<WyrdArthurUI>, ITreePatternHost {
        public WyrdTalentTreeSlotUI SelectedSlotUI { get; private set; }
        public VWyrdTalentTooltipSystemUI Tooltip => _tooltip;
        public Transform TreeParent => View.TreeParent;
        
        Hero Hero => ParentModel.Hero;
        VWyrdArthurPower View => View<VWyrdArthurPower>();

        VWyrdTalentTooltipSystemUI _tooltip;

        protected override void OnFullyInitialized() {
            Fill(Hero.Talents.TableOf(View.Tree));
            _tooltip = AddElement(new FloatingTooltipUI(typeof(VWyrdTalentTooltipSystemUI), View.transform, 0.2f)).View<VWyrdTalentTooltipSystemUI>();
        }
        
        void Fill(TalentTable table) {
            SelectedSlotUI = null;
            RemoveElementsOfType<WyrdTalentTreeSlotUI>();
            RemoveElementsOfType<TalentTreePatternElement>();
            
            if (table.TreeTemplate.Pattern != null && table.TreeTemplate.Pattern is VWyrdTalentTreePattern) {
                var pattern = AddElement(new TalentTreePatternElement(table.TreeTemplate));
                View.SetupPattern(pattern.View<VWyrdTalentTreePattern>());
                
                for (int i = 0; i < table.talents.Count; i++) {
                    var talentTreeSlot = AddElement(new WyrdTalentTreeSlotUI(table.talents[i]));
                    View.SpawnSlot(talentTreeSlot, i);
                }
            } else {
                Log.Important?.Error($"No pattern found for talent tree {table.TreeTemplate.name}");
            }
        }
        
        public void SelectTalent(WyrdTalentTreeSlotUI slot, bool state) {
            SelectedSlotUI = state ? slot : null;
            ParentModel.RefreshPrompts(SelectedSlotUI?.Talent);
        }
    }
}
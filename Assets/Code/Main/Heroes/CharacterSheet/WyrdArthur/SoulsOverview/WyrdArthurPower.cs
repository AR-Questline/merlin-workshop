using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern.Host;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items.Tooltips.Base;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.MVC;
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
        public SpriteReference LineSpriteReference => View.LineSpriteReference;
        public TalentTable CurrentTable { get; private set; }

        Hero Hero => ParentModel.Hero;
        VWyrdArthurPower View => View<VWyrdArthurPower>();

        VWyrdTalentTooltipSystemUI _tooltip;
        VTalentTreePatternHost PatternHost { get; set; }

        protected override void OnFullyInitialized() {
            Fill(Hero.Talents.TableOf(View.Tree));
            _tooltip = AddElement(new FloatingTooltipUI(typeof(VWyrdTalentTooltipSystemUI), View.transform, 0.2f)).View<VWyrdTalentTooltipSystemUI>();
        }
        
        void Fill(TalentTable table) {
            SelectedSlotUI = null;
            CurrentTable = table;
            RemoveElementsOfType<WyrdTalentTreeSlotUI>();
            if (PatternHost != null) PatternHost.Discard();

            if (table.TreeTemplate.PatternType != null) {
                PatternHost = World.SpawnView(this, CurrentTable.TreeTemplate.PatternType) as VTalentTreePatternHost;
                if (PatternHost != null) View.SetupPattern(PatternHost.Pattern as VWyrdTalentTreePattern);
                
                for (int i = 0; i < table.talents.Count; i++) {
                    var talent = CurrentTable.talents[i];
                    var talentTreeSlot = AddElement(new WyrdTalentTreeSlotUI(table.talents[i]));
                    var uiSlot = PatternHost.Pattern.GetSlotForTalent(talent);
            
                    if (uiSlot == null) {
                        Log.Critical?.Error($"No tree node found for talent {talent.Template.name} in pattern {PatternHost.Pattern.name}", View);
                        continue;
                    }
            
                    var slotView = World.SpawnView<VWyrdTalentTreeSlotUI>(talentTreeSlot, true, true, uiSlot);
                    View.SetupSlot(slotView, i);
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
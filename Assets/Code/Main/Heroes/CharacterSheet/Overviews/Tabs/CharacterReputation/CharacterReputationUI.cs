using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterReputation {
    public partial class CharacterReputationUI : CharacterSheetTab<VCharacterReputationUI> {
        CharacterSheetUI CharacterSheetUI => ParentModel;
        VCharacterReputationUI View => View<VCharacterReputationUI>();

        protected override void AfterViewSpawned(VCharacterReputationUI view) {
            CharacterSheetUI.SetHeroOnRenderVisible(false);
            InitializeReputation();
        }

        void InitializeReputation() {
            var humansFaction = World.Services.Get<FactionService>().Humans;
            // var reputationFactions =
            //     humansFaction.SubFactions.Where(f => FactionReputationUtil.HasReputation(f.Template));
            //
            // foreach (Faction faction in reputationFactions) {
            //     var factionEntry = new FactionEntryUI(faction);
            //     AddElement(factionEntry);
            //     factionEntry.ListenTo(FactionEntryUI.Events.FactionChanged, OnFactionChanged, this);
            //     World.SpawnView<VFactionEntryUI>(factionEntry, true);
            //
            //     foreach (Faction subFaction in faction.SubFactions) {
            //         if (!FactionReputationUtil.HasReputation(subFaction.Template)) {
            //             continue;
            //         }
            //
            //         var subFactionEntry = new FactionEntryUI(subFaction);
            //         AddElement(subFactionEntry);
            //         subFactionEntry.ListenTo(FactionEntryUI.Events.FactionChanged, OnFactionChanged, this);
            //         World.SpawnView<VFactionEntryNestedUI>(subFactionEntry, true);
            //     }
            // }

            OnFactionChanged(TryGetElement<FactionEntryUI>());
            FocusFirstItem().Forget();
        }
        
        async UniTaskVoid FocusFirstItem() {
            if (await AsyncUtil.DelayFrame(this)) {
                var firstEntry = Elements<FactionEntryUI>().FirstOrDefault();
                if (firstEntry) {
                    World.Only<Focus>().Select(firstEntry.View<VFactionEntryUI>().Button);
                }
            }
        }

        void OnFactionChanged(FactionEntryUI factionEntry) {
            if (factionEntry == null) {
                return;
            }
            
            View.RefreshFactionInfo(factionEntry);
        }
    }
}
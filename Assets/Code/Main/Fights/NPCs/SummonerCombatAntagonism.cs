using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Fights.NPCs {
    public partial class SummonerCombatAntagonism : Element<NpcElement> {
        public override ushort TypeForSerialization => SavedModels.SummonerCombatAntagonism;

        readonly HashSet<AntagonismMarker> _ownerAntagonisms = new();

        // === Initialization
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<NpcAISummon>(), this, OnAllyAdded);
            ParentModel.ListenTo(Events.AfterElementsCollectionModified, FactionAntagonismChanged, this);
            foreach (var factionAntagonism in Elements<AntagonismMarker>()) {
                FactionAntagonismChanged(factionAntagonism);
            }
        }

        void FactionAntagonismChanged(Element element) {
            if (element is not AntagonismMarker antagonism) {
                return;
            }
            
            if (element.HasBeenDiscarded) {
                _ownerAntagonisms.Remove(antagonism);
                return;
            }

            if (antagonism is FactionAntagonism factionAntagonism) {
                ApplyNewFactionAntagonismToAllAllies(factionAntagonism);
            } else if (antagonism is CharacterAntagonism characterAntagonism) {
                ApplyNewCharacterAntagonismToAllAllies(characterAntagonism);
            }
            
            _ownerAntagonisms.Add(antagonism);
        }
        
        // === Applying Antagonism
        void ApplyNewFactionAntagonismToAllAllies(FactionAntagonism antagonism) {
            foreach (var ally in World.All<NpcAlly>().Where(a => a.Ally == ParentModel)) {
                ApplyFactionAntagonism(antagonism.Faction, ally.ParentModel);
            }
        }
        
        void ApplyNewCharacterAntagonismToAllAllies(CharacterAntagonism antagonism) {
            foreach (var ally in World.All<NpcAlly>().Where(a => a.Ally == ParentModel)) {
                ApplyCharacterAntagonism(antagonism.Character, ally.ParentModel);
            }
        }

        void OnAllyAdded(Model model) {
            NpcAISummon ally = (NpcAISummon)model;
            if (ally.Owner != ParentModel) {
                return;
            }
            
            foreach (var antagonism in _ownerAntagonisms) {
                if (antagonism is FactionAntagonism factionAntagonism) {
                    ApplyFactionAntagonism(factionAntagonism.Faction, ally.ParentModel);
                } else if (antagonism is CharacterAntagonism characterAntagonism) {
                    ApplyCharacterAntagonism(characterAntagonism.Character, ally.ParentModel);
                }
            }
        }

        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            _ownerAntagonisms.Clear();
        }

        // === Helpers
        void ApplyFactionAntagonism(Faction towards, ICharacter target) {
            AntagonismMarker.TryApplySingleton(
                new FactionAntagonism(AntagonismLayer.Default, AntagonismType.Mutual, towards, Antagonism.Hostile),
                new UntilIdle(ParentModel),
                target);
        }
        
        void ApplyCharacterAntagonism(ICharacter towards, ICharacter target) {
            AntagonismMarker.TryApplySingleton(
                new CharacterAntagonism(AntagonismLayer.Default, AntagonismType.Mutual, towards, Antagonism.Hostile),
                new UntilIdle(ParentModel),
                target
            );
        }
    }
}
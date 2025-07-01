using System.Collections.Generic;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroCombatAntagonism : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        readonly HashSet<NpcElement> _npcsWithFactionAntagonism = new();
        readonly HashSet<CharacterAntagonism> _myCharacterAntagonisms = new();

        // === Initialization
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<NpcAlly>(), this, OnAllyAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<CharacterAntagonism>(), this, OnCharacterAntagonismAdded);
        }

        // === Applying Antagonism
        public void ApplyCombatAntagonism(NpcElement npc) {
            bool canReact = !npc.IsUnconscious;
            if (canReact) {
                if (ApplyFactionAntagonism(npc, Hero.Current)) {
                    ApplyNewFactionAntagonismToAllAllies(npc);
                    bool npcAdded = _npcsWithFactionAntagonism.Add(npc);
                    if (npcAdded) {
                        npc.ListenTo(Events.AfterDiscarded, OnNpcDiscarded, this);
                    }
                }
            }
        }
        
        void ApplyNewFactionAntagonismToAllAllies(NpcElement npc) {
            foreach (var ally in World.All<NpcAlly>().Where(a => a.Ally == Hero.Current)) {
                ApplyFactionAntagonism(npc, ally.ParentModel);
            }
        }

        void OnAllyAdded(Model model) {
            NpcAlly ally = (NpcAlly)model;
            if (ally.Ally != Hero.Current) {
                return;
            }

            NpcElement allyNpc = ally.ParentModel;
            
            foreach (var npc in _npcsWithFactionAntagonism) {
                ApplyFactionAntagonism(npc, allyNpc);
            }

            foreach (var antagonism in _myCharacterAntagonisms) {
                ApplyCharacterAntagonism(antagonism.ParentModel, allyNpc, false);
            }
        }

        void OnCharacterAntagonismAdded(Model model) {
            CharacterAntagonism characterAntagonism = (CharacterAntagonism) model;
            if (characterAntagonism.Character != ParentModel) {
                return;
            }

            _myCharacterAntagonisms.Add(characterAntagonism);
            characterAntagonism.ListenTo(Events.AfterDiscarded, OnCharacterAntagonismDiscarded, this);
            ApplyNewCharacterAntagonismToAllAllies(characterAntagonism.ParentModel);
        }

        void ApplyNewCharacterAntagonismToAllAllies(ICharacter towards) {
            foreach (var ally in World.All<NpcAlly>().Where(a => a.Ally == Hero.Current)) {
                ApplyCharacterAntagonism(towards, ally.ParentModel, true);
            }
        }
        
        void OnNpcDiscarded(Model model) {
            _npcsWithFactionAntagonism.Remove((NpcElement)model);
        }
        
        void OnCharacterAntagonismDiscarded(Model model) {
            _myCharacterAntagonisms.Remove((CharacterAntagonism)model);
        }
        
        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            _npcsWithFactionAntagonism.Clear();
            _myCharacterAntagonisms.Clear();
        }

        // === Helpers
        static bool ApplyFactionAntagonism(NpcElement npc, ICharacter target) {
            return AntagonismMarker.TryApplySingleton(
                new FactionAntagonism(AntagonismLayer.Default, AntagonismType.Mutual, npc.Faction, Antagonism.Hostile),
                new UntilHeroEndOfCombat(),
                target
            );
        }
        
        void ApplyCharacterAntagonism(ICharacter towards, NpcElement target, bool tryEnterCombat) {
            if (towards is NpcElement npc) {
                AntagonismMarker.TryApplySingleton(
                    new CharacterAntagonism(AntagonismLayer.Default, AntagonismType.Mutual, towards, Antagonism.Hostile),
                    new UntilIdle(npc),
                    target
                );
            } else {
                AntagonismMarker.TryApplySingleton(
                    new CharacterAntagonism(AntagonismLayer.Default, AntagonismType.Mutual, towards, Antagonism.Hostile),
                    new UntilHeroEndOfCombat(),
                    target
                );
            }
            
            if (tryEnterCombat && target.GetCurrentTarget() == null) {
                target.NpcAI.EnterCombatWith(towards);
            }
        }
    }
}
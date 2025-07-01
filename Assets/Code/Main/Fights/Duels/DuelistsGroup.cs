using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Fights.Duels {
    public class DuelistsGroup {
        readonly int _groupId;
        readonly StoryBookmark _callbackOnGroupVictory;
        readonly Location _locationToStartCallback;
        int _keepingGroupAliveCount;
        
        public int Id => _groupId;
        public bool Defeated => _keepingGroupAliveCount == 0;
        public List<DuelistElement> Duelists { get; } = new();
        public DuelController Controller { get; }

        public DuelistsGroup(int groupId, StoryBookmark callbackOnGroupVictory, Location locationToStartCallback, DuelController controller) {
            _groupId = groupId;
            _callbackOnGroupVictory = callbackOnGroupVictory;
            _locationToStartCallback = locationToStartCallback;
            Controller = controller;
        }

        public DuelistElement AddDuelist(ICharacter character, DuelistSettings settings) {
            if (settings.keepsDuelAlive) {
                _keepingGroupAliveCount++;
            }

            var newDuelist = AddDuelistElement(this, character, settings);
            foreach (var duelist in Duelists) {
                duelist.ParentModel.TurnFriendlyTo(AntagonismLayer.Duel, character);
            }

            Duelists.Add(newDuelist);
            AddDuelistsSummons(character);
            return newDuelist;
        }
        
        static DuelistElement AddDuelistElement(DuelistsGroup group, ICharacter character, DuelistSettings settings) {
            switch (character) {
                case Hero:
                    return character.AddElement(new HeroDuelistElement(group, settings));
                case NpcElement:
                    return character.AddElement(new NpcDuelistElement(group, settings));
                default:
                    throw new Exception($"Trying to apply DuelistElement to unsupported character type {character.GetType().Name}");
            }
        }

        void AddDuelistsSummons(ICharacter character) {
            foreach (var npcSummon in World.All<INpcSummon>()) {
                if (!npcSummon.IsAlive) continue;
                
                if (npcSummon.Owner == character) {
                    Controller.AddDuelist(npcSummon.ParentModel,  _groupId, DuelistSettings.Summon);
                }
            }
        }
        
        public void RemoveDuelist(DuelistElement duelist) {
            Duelists.Remove(duelist);
        }

        public void TurnFriendlyTo(ICharacter other) {
            foreach (var duelist in Duelists) {
                duelist.ParentModel.TurnFriendlyTo(AntagonismLayer.Duel, other);
            }
        }
        
        public void TurnHostileTo(ICharacter other) {
            foreach (var duelist in Duelists) {
                duelist.ParentModel.TurnHostileTo(AntagonismLayer.Duel, other);
            }
        }

        public void StartDuel() {
            foreach (var duelist in Duelists.ToArray()) {
                duelist.StartDuel();
            }
        }

        public void EndDuel() {
            foreach (var duelist in Duelists.ToArray()) {
                duelist.EndDuel();
            }
            Duelists.Clear();
        }

        public virtual void OnDuelistDefeated(DuelistElement duelist) {
            if (duelist.Settings.keepsDuelAlive) {
                _keepingGroupAliveCount--;
                if (_keepingGroupAliveCount == 0) {
                    AllDuelistsDefeated();
                }
            }
        }

        void AllDuelistsDefeated() {
            foreach (var duelist in Duelists) {
                if (duelist is NpcDuelistElement { HasBeenDiscarded: false } npcDuelist && !npcDuelist.HasElement<DefeatedNpcDuelistElement>()) {
                    npcDuelist.Defeat(true);
                }
            }
            Controller.AllDuelistsDefeated();
        }

        public void VictoriousDuel() {
            foreach (var duelist in Duelists) {
                duelist.Victory();
            }
            if (_callbackOnGroupVictory is { IsValid: true }) {
                var config = _locationToStartCallback is { HasBeenDiscarded: false } 
                        ? StoryConfig.Location(_locationToStartCallback, _callbackOnGroupVictory, typeof(VDialogue)) 
                        : StoryConfig.Base(_callbackOnGroupVictory, typeof(VDialogue));
                var story = Story.StartStory(config);
                if (story == null) {
                    Log.Critical?.Error($"Duel ended, but callback story wasn't created properly. Bookmark: {_callbackOnGroupVictory.GUID}. Victorious Duelists: {string.Join(", ", Duelists.Select(d => d?.ParentModel?.Name))}");
                    if (Controller is { AutoEnd: false }) {
                        Controller.EndDuel();
                    }
                }
            }
        }
    }
}

using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Fights.NPCs {
    public partial class NpcDifficultyStatModifier : Element<Location>, IRefreshedByAttachment<NpcDifficultyStatModifierAttachment> {
        public override ushort TypeForSerialization => SavedModels.NpcDifficultyStatModifier;

        NpcElement _modifiedNpc;
        NpcDifficultyStatModifierAttachment _modifierSpec;
        readonly List<StatTweak> _currentTweaks = new();
        
        public void InitFromAttachment(NpcDifficultyStatModifierAttachment spec, bool isRestored) {
            _modifierSpec = spec;
        }

        protected override void OnFullyInitialized() {
            ParentModel.AfterFullyInitialized(OnLocationFullyInitialized);
            
            var difficultySetting = World.Only<DifficultySetting>();
            difficultySetting.ListenTo(Setting.Events.SettingRefresh, OnDifficultyChanged, this);
        }

        void OnLocationFullyInitialized() {
            if (ParentModel.TryGetElement(out NpcPresence presence)) {
                OnAttachedToPresence(presence);
            } else if (ParentModel.TryGetElement(out NpcElement npc)) {
                OnAttachedToNpc(npc);
            } else {
                Discard();
            }
        }

        void OnAttachedToPresence(NpcPresence presence) {
            _modifiedNpc = presence.AliveNpc;
            presence.ListenTo(NpcPresence.Events.AttachedNpc, RefreshTweaksForCurrentDifficulty, this);
            presence.ListenTo(NpcPresence.Events.DetachedNpc, ClearTweaks, this);
            RefreshTweaksForCurrentDifficulty();
        }

        void OnAttachedToNpc(NpcElement npc) {
            _modifiedNpc = npc;
            RefreshTweaksForCurrentDifficulty();
        }
        
        void OnDifficultyChanged(Setting setting) {
            var difficultySetting = (DifficultySetting)setting;
            RefreshTweaksForDifficulty(difficultySetting.Difficulty);
        }

        void RefreshTweaksForDifficulty(Difficulty difficulty) {
            ClearTweaks();

            if (_modifiedNpc == null) {
                return;
            }
            
            foreach (var statInfo in _modifierSpec.GetStatsForDifficulty(difficulty)) {
                var stat = _modifiedNpc.Stat(statInfo.statType);
                if (stat == null) {
                    Log.Minor?.Error($"Cannot modify non-existing stat {statInfo.statType} in NPC {_modifiedNpc}!", _modifiedNpc.ParentModel.Spec);
                    continue;
                }
                var tweak = StatTweak.Multi(stat, statInfo.multiplier, parentModel: _modifiedNpc);
                tweak.MarkedNotSaved = true;
                _currentTweaks.Add(tweak);
            }
        }

        void RefreshTweaksForCurrentDifficulty() {
            var difficultySetting = World.Only<DifficultySetting>();
            RefreshTweaksForDifficulty(difficultySetting.Difficulty);
        }
        
        void ClearTweaks() {
            foreach (var tweak in _currentTweaks) {
                tweak.Discard();
            }
            _currentTweaks.Clear();
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ClearTweaks();
            _modifiedNpc = null;
        }
        
        public struct StatInfo {
            public StatType statType;
            public float multiplier;
            
            public StatInfo(StatType statType, float multiplier) {
                this.statType = statType;
                this.multiplier = multiplier;
            }
        }
    }
}
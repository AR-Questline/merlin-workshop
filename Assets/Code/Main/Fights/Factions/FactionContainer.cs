using System;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Fights.Factions {
    /// <summary>
    /// Class that contains all faction-related information.
    /// It also handles overriding faction.
    /// </summary>
    public partial class FactionContainer {
        public ushort TypeForSerialization => SavedTypes.FactionContainer;

        /// <summary> Default Faction taken from template. IWithFaction must explicitly set it OnInitialize and OnRestore. </summary>
        /// <see cref="SetDefaultFaction"/>
        FactionTemplate _default;
        
        /// <summary> FactionOverride. If there is none Faction is considered _default; </summary>
        /// <see cref="OverrideFaction"/>
        /// <see cref="ResetFactionOverride"/>
        [Saved] StructList<FactionOverride> _overrides;
        
        /// <summary> Cached Faction. After every change of _default or _override must be Refreshed </summary>
        /// <see cref="RefreshFaction"/>
        Faction _faction;

        /// <summary> Get current faction considering all factors and overrides </summary>
        public Faction Faction => _faction;

        /// <summary> Shall be used only by IWithFaction to init default faction so it matches the one in template </summary>
        public void SetDefaultFaction(FactionTemplate faction) {
            _default = faction;
            RefreshFaction();
        }

        public FactionTemplate GetFactionTemplateForSummon() {
            if (_overrides is { IsCreated: true, Count: > 0 }) {
                foreach (var factionOverride in _overrides) {
                    if (factionOverride.context == FactionOverrideContext.Duel) {
                        continue;
                    }
                    return factionOverride.faction;
                }
            }
            return _default;
        }
        
        public void OverrideFaction(FactionTemplate faction, FactionOverrideContext context = FactionOverrideContext.Default) {
            var factionOverride = new FactionOverride() {
                faction = faction,
                context = context
            };
            
            if (!_overrides.IsCreated) {
                _overrides = new StructList<FactionOverride>(1);
                _overrides.Add(factionOverride);
            } else if (_overrides.Count == 0) {
                _overrides.Add(factionOverride);
            } else if (_overrides[^1].context == context) {
                // Replace previous override with the new one if is the newest override.
                _overrides[^1] = factionOverride;
            } else {
                int overrideIndex = -1;
                for (int i = _overrides.Count - 1; i >= 0; i--) {
                    if (_overrides[i].context == context) {
                        overrideIndex = i;
                        break;
                    }
                }
                if (overrideIndex == -1) {
                    // Override with this context is not present
                    _overrides.Add(factionOverride);
                } else {
                    // Override with this context is present, move whole list to the left and place new override as the newest.
                    for (int i = overrideIndex; i < _overrides.Count - 2; i++){
                        _overrides[i] = _overrides[i + 1];
                    }
                    _overrides[^1] = factionOverride;
                }
            }
            
            RefreshFaction();
        }

        public void ResetFactionOverride(FactionOverrideContext context = FactionOverrideContext.Default) {
            if (_overrides is { IsCreated: false } or { Count: 0 }) {
                return;
            }
            
            for (int i = _overrides.Count - 1; i >= 0; i--) {
                if (_overrides[i].context == context) {
                    _overrides.RemoveAt(i);
                    break;
                }
            }

            RefreshFaction();
        }

        void RefreshFaction() {
            _faction = World.Services.Get<FactionService>().FactionByTemplate(_overrides is {IsCreated: true, Count: >0} ? _overrides[^1].faction : _default);
        }

        [Serializable]
        public partial struct FactionOverride {
            public ushort TypeForSerialization => SavedTypes.FactionOverride;

            [Saved] public FactionTemplate faction;
            [Saved] public FactionOverrideContext context;
        }
    }
    
    public enum FactionOverrideContext : byte {
        Default,
        Duel,
        Summon,
        Ally,
    }
}
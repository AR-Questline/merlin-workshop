using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Fights.Duels {
    public partial class DuelController : Model {
        public sealed override bool IsNotSaved => true;

        readonly bool _autoEnd;
        readonly DuelistSettings _defaultDuelistSettings;
        DuelistsGroup[] _duelistsGroups;
        SaveBlocker _blocker;
        IDuelArena _arena;
        bool _started;

        public override Domain DefaultDomain => Domain.Gameplay;
        public bool Started => _started;
        public bool AutoEnd => _autoEnd;

        public DuelController(bool autoEnd, DuelistSettings defaultDuelistSettings) {
            _autoEnd = autoEnd;
            _defaultDuelistSettings = defaultDuelistSettings;
            _duelistsGroups = Array.Empty<DuelistsGroup>();
        }

        public void StartDuel() {
            _started = true;
            _blocker = World.Add(new SaveBlocker(this));
            for (int i = 0; i < _duelistsGroups.Length; i++) {
                foreach (var duelist in _duelistsGroups[i].Duelists) {
                    UpdateAntagonism(duelist, true);
                }
                _duelistsGroups[i].StartDuel();
            }
        }
        
        public void AddDuelGroup(IEnumerable<ICharacter> characterGroup, StoryBookmark callbackOnGroupVictory, Location locationToStartCallback, DuelistSettings? settings = null) {
            var oldArray = _duelistsGroups;
            int oldArraySize = oldArray.Length;
            ArrayUtils.Add(ref _duelistsGroups, new DuelistsGroup(oldArraySize, callbackOnGroupVictory, locationToStartCallback, this));
            foreach (var character in characterGroup) {
                AddDuelist(character, oldArraySize, settings ?? _defaultDuelistSettings);
            }
        }
        
        public void AddDuelist(ICharacter character, int group, DuelistSettings? settings = null) {
            bool isSummon = character is NpcElement { IsSummon: true };
            var fallbackDuelistSettings = isSummon ? DuelistSettings.Summon : _defaultDuelistSettings;
            
            if (group < 0 || group > _duelistsGroups.Length - 1) {
                Log.Minor?.Error($"Cannot add {character} to group {group} because group with this ID doesnt exist");
                return;
            }
            var duelist = _duelistsGroups[group].AddDuelist(character, settings ?? fallbackDuelistSettings);
            if (_started) {
                UpdateAntagonism(duelist, false);
                duelist.StartDuel();
            }
        }
        
        public void AllDuelistsDefeated() {
            var stillFightingGroups = _duelistsGroups.Where(g => !g.Defeated).ToArray();
            if (stillFightingGroups.Length != 1) {
                return;
            }
            stillFightingGroups[0].VictoriousDuel();
            if (_autoEnd) {
                EndDuel();
            }
        }
        
        public ICharacter FindFirstTargetForNpc(NpcElement duelist, int group) {
            for (int i = group - 1; i >= 0; i--) {
                if (_duelistsGroups[i].Defeated) {
                    continue;
                }
                var target = _duelistsGroups[i].Duelists.FirstOrDefault(d => !d.Defeated);
                if (target is { ParentModel: { HasBeenDiscarded: false } }) {
                    return target.ParentModel;
                }
            }
            return null;
        }
        
        public bool FindAnyTargetForNpc(NpcElement duelist, int group) {
            var target = GetHostileTarget(group);
            if (target is { ParentModel: { HasBeenDiscarded: false } }) {
                duelist.NpcAI?.EnterCombatWith(target.ParentModel, true);
                return true;
            }

            return false;
        }

        public DuelistElement GetHostileTarget(int group) {
            for (int i =  0; i < _duelistsGroups.Length; i++) {
                if (_duelistsGroups[i].Defeated || i == group) {
                    continue;
                }
                var target = _duelistsGroups[i].Duelists.FirstOrDefault(d => !d.Defeated);
                if (target is { ParentModel: { HasBeenDiscarded: false } }) {
                    return target;
                }
            }
            return null;
        }

        public void EndDuel() {
            foreach (var group in _duelistsGroups) {
                group.EndDuel();
            }
            Discard();
        }
        
        public void ClearAntagonismTowards(DuelistElement duelist, int characterGroup) {
            ICharacter character = duelist.ParentModel;
            if (character is not { HasBeenDiscarded: false }) {
                return;
            }
            for (int i = 0; i < _duelistsGroups.Length; i++) {
                if (_duelistsGroups[i] == null) {
                    continue;
                }
                for (int j = 0; j < _duelistsGroups[i].Duelists.Count; j++) {
                    if (_duelistsGroups[i].Duelists[j].ParentModel is not { HasBeenDiscarded: false }) {
                        continue;
                    }
                    if (i == characterGroup) {
                        _duelistsGroups[i].Duelists[j].ParentModel.ClearAllFriendshipWith(AntagonismLayer.Duel, character);
                    } else {
                        _duelistsGroups[i].Duelists[j].ParentModel.ClearAllHostilityWith(AntagonismLayer.Duel, character);
                    }
                }
            }
        }
        
        void UpdateAntagonism(DuelistElement duelist, bool onDuelStart) {
            ICharacter character = duelist.ParentModel;
            int characterGroup = duelist.GroupId;
            for (int i = onDuelStart ? characterGroup : 0; i < _duelistsGroups.Length; i++) {
                if (_duelistsGroups[i] == null) {
                    continue;
                }
                if (i == characterGroup) {
                    _duelistsGroups[i].TurnFriendlyTo(character);
                } else {
                    _duelistsGroups[i].TurnHostileTo(character);
                }
            }
        }

        // Arena
        
        public async UniTask AssignArena(DuelArenaData data, bool teleportToArenaScene = true, bool teleportToArena = true, bool activate = true) {
            if (_arena != null) {
                Log.Minor?.Error("Cannot assign arena, because another Duel Arena is activated");
                return;
            }

            if (teleportToArenaScene && data.sceneRef is { IsSet: true } 
                                     && !World.Services.Get<SceneService>().ActiveSceneRef.Equals(data.sceneRef)) {
                if (World.Services.Get<TransitionService>() is { } transitionService) {
                    transitionService.ToCamera(0f).Forget();
                    transitionService.TransitionFromBlack(0f).Forget();
                    await ScenePreloader.ChangeMapAndWait(data.sceneRef, LoadingScreenUI.Events.SceneInitializationEnded, this);
                    if (HasBeenDiscarded) {
                        return;
                    }
                    if (teleportToArena) {
                        transitionService.ToBlack(0f).Forget();
                        if (!await AsyncUtil.DelayFrame(this, 2)) {
                            return;
                        }
                    }
                } else {
                    await ScenePreloader.ChangeMapAndWait(data.sceneRef, LoadingScreenUI.Events.SceneInitializationEnded, this);
                    if (HasBeenDiscarded) {
                        return;
                    }
                }
            }

            var arenaLocation = data.arenaRef.MatchingLocations(null)?.FirstOrDefault();
            if (arenaLocation == null) {
                Log.Minor?.Error("Cannot teleport to arena, because there's no matching location");
                return;
            }

            if (!arenaLocation.TryGetElement<IDuelArena>(out var arena)) {
                Log.Minor?.Error("Cannot teleport to arena, because there's no IDuelArena in the matching location");
                return;
            }

            _arena = arena;
            
            if (teleportToArena) {
                await _arena.Teleport(_duelistsGroups, teleportToArenaScene);
                if (!await AsyncUtil.DelayFrame(this, 1)) {
                    return;
                }
            }
            if (activate) {
                _arena.Activate();
            }
        }

        public bool TryActivateArena() {
            if (_arena == null) {
                Log.Minor?.Error("Cannot activate to arena, because Duel Arena is not assigned. Use AssignArena first.");
                return false;
            }
            
            _arena.Activate();
            return true;
        }

        // Lifecycle
        
        protected override void OnDiscard(bool fromDomainDrop) {
            _arena?.Deactivate();
            _arena = null;
            if (_blocker == null) {
                Log.Critical?.Error("Discarding DuelController before it was started. Player is most likely in an invalid state that allows saving");
            } else {
                _blocker?.Discard();
                _blocker = null;
            }
        }
    }
}

using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Fights.Duels {
    [Serializable]
    public struct DuelArenaReferenceData {
        public ArenaDataSource arenaDataSource;
        [ShowIf(nameof(ShowFactionTemplate)), TemplateType(typeof(CrimeOwnerTemplate))] public TemplateReference arenaFaction;
        [ShowIf(nameof(ShowSceneRef))] public SceneReference sceneRef;
        [ShowIf(nameof(ShowLocationRef))] public LocationReference locationRef;
        
        bool ShowFactionTemplate => arenaDataSource is ArenaDataSource.Faction;
        bool ShowSceneRef => arenaDataSource is ArenaDataSource.Custom;
        bool ShowLocationRef => arenaDataSource is ArenaDataSource.Actor or ArenaDataSource.Custom;

        public bool TryGetArenaData(Story api, out DuelArenaData data) {
            switch (arenaDataSource) {
                case ArenaDataSource.Faction:
                    data = arenaFaction.Get<CrimeOwnerTemplate>().DuelArena;
                    return true;
                case ArenaDataSource.Actor:
                    var location = locationRef.MatchingLocations(api).FirstOrDefault();
                    if (location == null) {
                        Log.Important?.Error("No matching location to get duel arena");
                        data = default;
                        return false;
                    }
                    if (location.GetCurrentCrimeOwnersFor(CrimeArchetype.Combat(CrimeNpcValue.Low)) is {IsEmpty: false} crimeOwners) {
                        data = crimeOwners.PrimaryOwner.DuelArena;
                        return true;
                    } else {
                        Log.Important?.Error($"{location} has no CrimeOwner to get duel arena");
                        data = default;
                        return false;
                    }
                case ArenaDataSource.Custom:
                    data = new DuelArenaData() {
                        sceneRef = sceneRef,
                        arenaRef = locationRef
                    };
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
            
        [Serializable]
        public enum ArenaDataSource : byte {
            Faction,
            Actor,
            Custom
        }
    }
}

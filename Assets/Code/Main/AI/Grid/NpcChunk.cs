using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Unity.Mathematics;

namespace Awaken.TG.Main.AI.Grid {
    public class NpcChunk : INpcChunk<NpcElement>, INpcChunk<Corpse>, INpcChunk<NpcDummy>, INpcChunk<AliveLocation> {
        DeferredCoordsChangingData _coordsChangingData;

        [UnityEngine.Scripting.Preserve] NpcGrid _grid;
        NpcChunkData _data;
        bool _dirtyData;
        Hero _hero;

        public int2 Coords { get; private set; }
        public List<NpcElement> Npcs { get; } = new();
        public List<AliveLocation> AliveLocations { get; } = new();
        public List<Corpse> Corpses { get; } = new();
        public List<NpcDummy> Dummies { get; } = new();
        public ref readonly NpcChunkData Data => ref _data;

        public ref readonly DeferredCoordsChangingData CoordsChangingData => ref _coordsChangingData;
        
        public Hero Hero {
            get => _hero;
            set {
                _hero = value;
                _data.SetHasHero(_hero != null);
            }
        }

        public NpcChunk(NpcGrid grid, int2 coords) {
            _grid = grid;
            Coords = coords;
        }

        // === Moving
        
        public void BeginDeferredCoordsChanging(int2 coords) {
            _coordsChangingData.isChanging = true;
            if (Coords.Equals(coords)) {
                return;
            }

            _data.Clear();
            RemoveNpcsOnCoordsChange();
            RemoveAliveLocationOnCoordsChange();
            Corpses.Clear();
            Dummies.Clear();
            AliveLocations.Clear();
            Coords = coords;
            _coordsChangingData.isChangingPosition = true;
        }

        public void FinishDeferredCoordsChanging() {
            _coordsChangingData = default;
        }
        
        // === Updating

        public void Update(float deltaTime) {
            if (_dirtyData) {
                _data.RefreshCombatDanger(this);
                _dirtyData = false;
            }
            _data.UpdateLocalDanger(deltaTime);
        }
        
        public void UpdateDangerSpread(in Neighbours neighbours) {
            _data.UpdateSpreadDanger(neighbours);
        }

        public void ResetDangerSpread() {
            _data.ResetDangerSpread();
        }

        public void NotifyDangerousEvent(bool isDangerToFearfuls) {
            _data.NotifyDangerousEvent(isDangerToFearfuls);
        }
        
        // === Npc Handling

        public void AddNpc(NpcElement npc) {
            npc.NpcChunk = this;
            Npcs.Add(npc);
            _dirtyData = true;
        }

        public void RemoveNpc(NpcElement npc) {
            npc.NpcChunk = null;
            Npcs.Remove(npc);
            _dirtyData = true;
        }

        public void NpcStateChanged(NpcElement npc) {
            _dirtyData = true;
        }

        void RemoveNpcsOnCoordsChange() {
            foreach (var npc in Npcs) {
                npc.NpcChunk = null;
            }
            Npcs.Clear();
            _dirtyData = true;
        }
        
        // === AliveLocations Handling
        
        public void AddAliveLocation(AliveLocation alive) {
            alive.NpcChunk = this;
            AliveLocations.Add(alive);
        }

        public void RemoveAliveLocation(AliveLocation alive) {
            alive.NpcChunk = null;
            AliveLocations.Remove(alive);
        }

        void RemoveAliveLocationOnCoordsChange() {
            foreach (var alive in AliveLocations) {
                alive.NpcChunk = null;
            }
            AliveLocations.Clear();
        }
        
        // === Corpses Handling

        public void AddCorpse(Corpse corpse) {
            corpse.NpcChunk = this;
            Corpses.Add(corpse);
        }

        public void RemoveCorpse(Corpse corpse) {
            corpse.NpcChunk = null;
            Corpses.Remove(corpse);
        }

        // === NpcDummies Handling
        
        public void AddNpcDummy(NpcDummy dummy) {
            dummy.NpcChunk = this;
            Dummies.Add(dummy);
        }

        public void RemoveNpcDummy(NpcDummy dummy) {
            dummy.NpcChunk = null;
            Dummies.Remove(dummy);
        }

        // === INpcChunk

        List<NpcElement> INpcChunk<NpcElement>.GetEntries() => Npcs;
        List<Corpse> INpcChunk<Corpse>.GetEntries() => Corpses;
        List<NpcDummy> INpcChunk<NpcDummy>.GetEntries() => Dummies;
        List<AliveLocation> INpcChunk<AliveLocation>.GetEntries() => AliveLocations;
        
        // === Helpers
        
        public struct DeferredCoordsChangingData {
            [UnityEngine.Scripting.Preserve] public bool isChanging;
            public bool isChangingPosition;
        }

        public struct Neighbours {
            public NpcChunk neighbour0;
            public NpcChunk neighbour1;
            public NpcChunk neighbour2;
            public NpcChunk neighbour3;
            public NpcChunk neighbour4;
            public NpcChunk neighbour5;
            public NpcChunk neighbour6;
            public NpcChunk neighbour7;
        }
    }
    
    public interface INpcChunk<TEntry> {
        List<TEntry> GetEntries();
    }
}
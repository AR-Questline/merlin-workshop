using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Grid.Iterators;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Grid {
    public class NpcGrid : IDomainBoundService {
        public Domain Domain => Domain.Gameplay;

        NpcGridSetupData _data;
        float _continuousHysteresis;

        NpcChunk[] _chunks;
        int2 _center;

        public int GridHalfSize => _data.gridHalfSize;
        public int GridSize => 2 * _data.gridHalfSize + 1;
        public float ChunkSize => _data.chunkSize;
        public ref readonly int2 Center => ref _center;
        public NpcChunk[] Chunks => _chunks;

        // === Initialization - Deinitialization

        public void Init() {
            World.EventSystem.PreAllocateMyListeners(this, 634);

            _data = GameConstants.Get.npcGrid;
            _continuousHysteresis = 0.5f + _data.hysteresis / ChunkSize;
            
            _center = GetCoordsOf(Hero.Current.Coords);
            CreateChunks();
            InitListeners();
            AssignHeroChunk();
        }

        public bool RemoveOnDomainChange() {
            DeinitListeners();
            return true;
        }

        void CreateChunks() {
            int gridSize = GridSize;
            _chunks = new NpcChunk[gridSize * gridSize];
            for (int x = _center.x - GridHalfSize; x <= _center.x + GridHalfSize; x++) {
                for (int y = _center.y - GridHalfSize; y <= _center.y + GridHalfSize; y++) {
                    var coords = new int2(x, y);
                    GetChunkUnchecked(coords) = new NpcChunk(this, coords);
                }
            }
            foreach (var npc in World.All<NpcElement>()) {
                OnNpcInitialized(npc);
            }
            foreach (var alive in World.All<AliveLocation>()) {
                OnAliveLocationInitialized(alive);
            }
            foreach (var corpse in World.All<Corpse>()) {
                if (TryGetChunk(corpse.Coords, out var chunk)) {
                    chunk.AddCorpse(corpse);
                }
            }
            foreach (var dummy in World.All<NpcDummy>()) {
                if (TryGetChunk(dummy.Coords, out var chunk)) {
                    chunk.AddNpcDummy(dummy);
                }
            }
        }

        void InitListeners() {
            Hero.Current.ListenTo(GroundedEvents.AfterMovedToPosition, OnHeroMoved, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<NpcElement>(), this, OnNpcInitialized);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<NpcElement>(), this, OnNpcDiscarded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<AliveLocation>(), this, OnAliveLocationInitialized);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<AliveLocation>(), this, OnAliveLocationDiscarded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<Corpse>(), this, OnCorpseInitialized);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<Corpse>(), this, OnCorpseDiscarded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<NpcDummy>(), this, OnNpcDummyInitialized);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<NpcDummy>(), this, OnNpcDummyDiscarded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<NpcDummy>(), this, OnNpcDummyDiscarded);
            World.EventSystem.ListenTo(EventSelector.AnySource, NpcDangerTracker.Events.CharacterDangerNearby, this, OnCharacterDanger);
            World.EventSystem.ListenTo(EventSelector.AnySource, UnconsciousElement.Events.UnconsciousKilled, this, OnUnconsciousNpcKilled);
            NpcState.Changed += OnNpcStateChanged;
        }

        void AssignHeroChunk() {
            var heroChunk = GetChunkUnchecked(_center);
            heroChunk.Hero = Hero.Current;
            Hero.Current.NpcChunk = heroChunk;
        }

        void DeinitListeners() {
            NpcState.Changed -= OnNpcStateChanged;
        }

        void OnNpcInitialized(Model model) {
            OnNpcInitialized((NpcElement)model);
        }

        void OnNpcInitialized(NpcElement npc) {
            if (TryGetChunk(npc.Coords, out var chunk)) {
                chunk.AddNpc(npc);
            }
            npc.ListenTo(GroundedEvents.AfterMoved, OnNpcMoved, this);
        }

        void OnNpcDiscarded(Model model) {
            var npc = (NpcElement)model;
            npc.NpcChunk?.RemoveNpc(npc);
        }

        void OnAliveLocationInitialized(Model model) {
            OnAliveLocationInitialized((AliveLocation)model);
        }

        void OnAliveLocationInitialized(AliveLocation alive) {
            if (TryGetChunk(alive.Coords, out var chunk)) {
                chunk.AddAliveLocation(alive);
            }
            alive.ListenTo(GroundedEvents.AfterMoved, OnAliveLocationMoved, this);
        }

        void OnAliveLocationDiscarded(Model model) {
            var alive = (AliveLocation)model;
            alive.NpcChunk?.RemoveAliveLocation(alive);
        }

        void OnCorpseInitialized(Model model) {
            var corpse = (Corpse)model;
            if (TryGetChunk(corpse.Coords, out var chunk)) {
                chunk.AddCorpse(corpse);
            }
        }
        
        void OnCorpseDiscarded(Model model) {
            var corpse = (Corpse)model;
            corpse.NpcChunk?.RemoveCorpse(corpse);
        }
        
        void OnNpcDummyInitialized(Model model) {
            var dummy = (NpcDummy)model;
            if (TryGetChunk(dummy.Coords, out var chunk)) {
                chunk.AddNpcDummy(dummy);
            }
        }
        
        void OnNpcDummyDiscarded(Model model) {
            var dummy = (NpcDummy)model;
            dummy.NpcChunk?.RemoveNpcDummy(dummy);
        }

        void OnNpcStateChanged(NpcElement npc, IState previous, IState current) {
            npc.NpcChunk?.NpcStateChanged(npc);
        }
        
        // === Access

        public bool TryGetChunk(int2 coords, out NpcChunk chunk) {
            var localCoords = coords - _center;
            bool inBounds = localCoords.x >= -GridHalfSize & localCoords.x <= GridHalfSize &
                            localCoords.y >= -GridHalfSize & localCoords.y <= GridHalfSize;
            chunk = inBounds ? GetChunkUnchecked(coords) : null;
            return inBounds;
        }

        public bool TryGetChunk(in Vector3 position, out NpcChunk chunk) {
            return TryGetChunk(GetCoordsOf(position), out chunk);
        }

        public int CalculateIndex(int2 coords) {
            int gridSize = GridSize;
            int gridSizeMultiple = gridSize << 16; // to avoid negative modulo
            return Mod(coords.x) * gridSize + Mod(coords.y);
            
            int Mod(int x) => (x + gridSizeMultiple) % gridSize;
        }
        
        public ref NpcChunk GetChunkUnchecked(int2 coords) {
            return ref _chunks[CalculateIndex(coords)];
        }

        // === Update

        public void Update(float deltaTime) {
            foreach (var chunk in _chunks) {
                chunk.Update(deltaTime);
            }
            SpreadDanger();
        }

        void SpreadDanger() {
            int xMin = _center.x - GridHalfSize + 1;
            int xMax = _center.x + GridHalfSize - 1;
            int yMin = _center.y - GridHalfSize + 1;
            int yMax = _center.y + GridHalfSize - 1;
            
            NpcChunk chunk0X, chunk1X, chunk2X;
            NpcChunk chunk3X, chunk4X, chunk5X;
            // 6x, 7x, 8x are not used
            
            NpcChunk chunk0Y, chunk1Y, chunk2Y;
            NpcChunk chunk3Y, chunk4Y, chunk5Y;
            NpcChunk chunk6Y, chunk7Y, chunk8Y;
            
            chunk1X = GetChunkUnchecked(new int2(xMin - 1, yMin - 1));
            chunk4X = GetChunkUnchecked(new int2(xMin - 1, yMin));
            chunk2X = GetChunkUnchecked(new int2(xMin, yMin - 1));
            chunk5X = GetChunkUnchecked(new int2(xMin, yMin));
            
            for (int x = xMin; x <= xMax; x++) {
                chunk0X = chunk1X;
                chunk3X = chunk4X;
                chunk1X = chunk2X;
                chunk4X = chunk5X;
                chunk2X = GetChunkUnchecked(new int2(x + 1, yMin - 1));
                chunk5X = GetChunkUnchecked(new int2(x + 1, yMin));

                chunk3Y = chunk0X;
                chunk4Y = chunk1X;
                chunk5Y = chunk2X;
                chunk6Y = chunk3X;
                chunk7Y = chunk4X;
                chunk8Y = chunk5X;
                
                for (int y = yMin; y <= yMax; y++) {
                    chunk0Y = chunk3Y;
                    chunk1Y = chunk4Y;
                    chunk2Y = chunk5Y;
                    chunk3Y = chunk6Y;
                    chunk4Y = chunk7Y;
                    chunk5Y = chunk8Y;
                    chunk6Y = GetChunkUnchecked(new int2(x - 1, y + 1));
                    chunk7Y = GetChunkUnchecked(new int2(x, y + 1));
                    chunk8Y = GetChunkUnchecked(new int2(x + 1, y + 1));
                    
                    chunk4Y.UpdateDangerSpread(new NpcChunk.Neighbours {
                        neighbour0 = chunk0Y,
                        neighbour1 = chunk1Y,
                        neighbour2 = chunk2Y,
                        neighbour3 = chunk3Y,
                        neighbour4 = chunk5Y,
                        neighbour5 = chunk6Y,
                        neighbour6 = chunk7Y,
                        neighbour7 = chunk8Y,
                    });
                }
            }
        }

        void OnHeroMoved(Vector3 position) {
            var hero = Hero.Current;
            var newCenter = GetCoordsOfWithHysteresis(hero.NpcChunk.Coords, position);
            if (!_center.Equals(newCenter)) {
                _center = newCenter;
                UpdateChunks();
                hero.NpcChunk.Hero = null;
                AssignHeroChunk();
            }
        }

        void OnNpcMoved(IGrounded grounded) {
            var npc = (NpcElement)grounded;
            var currentChunk = npc.NpcChunk;
            var coords = npc.Coords;
            if (currentChunk == null) {
                if (TryGetChunk(coords, out var newChunk)) {
                    newChunk.AddNpc(npc);
                }
            } else {
                var currentCoords = currentChunk.Coords;
                var newCoords = GetCoordsOfWithHysteresis(currentCoords, npc.Coords);
                if (newCoords.Equals(currentCoords)) {
                    return;
                }
                if (TryGetChunk(newCoords, out var newChunk)) {
                    if (npc.NpcChunk != newChunk) {
                        currentChunk.RemoveNpc(npc);
                        newChunk.AddNpc(npc);
                    }
                } else {
                    currentChunk.RemoveNpc(npc);
                }
            }
        }

        void OnAliveLocationMoved(IGrounded grounded) {
            var alive = (AliveLocation)grounded;
            var currentChunk = alive.NpcChunk;
            var coords = alive.Coords;
            if (currentChunk == null) {
                if (TryGetChunk(coords, out var newChunk)) {
                    newChunk.AddAliveLocation(alive);
                }
            } else {
                var currentCoords = currentChunk.Coords;
                var newCoords = GetCoordsOfWithHysteresis(currentCoords, alive.Coords);
                if (newCoords.Equals(currentCoords)) {
                    return;
                }
                if (TryGetChunk(newCoords, out var newChunk)) {
                    if (alive.NpcChunk != newChunk) {
                        currentChunk.RemoveAliveLocation(alive);
                        newChunk.AddAliveLocation(alive);
                    }
                } else {
                    currentChunk.RemoveAliveLocation(alive);
                }
            }
        }

        void OnCharacterDanger(NpcDangerTracker.DirectDangerData data) {
            if (TryGetChunk(data.attacker.Coords, out var chunk)) {
                bool fearfulWasAttacked = CrimeReactionUtils.IsFleeingPeasant(data.receiver);
                chunk.NotifyDangerousEvent(fearfulWasAttacked);
                if (TryGetChunk(data.receiver.Coords, out var chunk2)) {
                    if (chunk2 != chunk) {
                        chunk2.NotifyDangerousEvent(fearfulWasAttacked);
                    }
                }
            }
        }
        
        void OnUnconsciousNpcKilled(UnconsciousElement unconscious) {
            if (TryGetChunk(Hero.Current.Coords, out var chunk)) {
                chunk.NotifyDangerousEvent(true);
                if (TryGetChunk(unconscious.ParentModel.Coords, out var chunk2)) {
                    if (chunk2 != chunk) {
                        chunk2.NotifyDangerousEvent(true);
                    }
                }
            }
        }

        void UpdateChunks() {
            int xMin = _center.x - GridHalfSize;
            int xMax = _center.x + GridHalfSize;
            int yMin = _center.y - GridHalfSize;
            int yMax = _center.y + GridHalfSize;
            
            for (int x = xMin; x <= xMax; x++) {
                for (int y = yMin; y <= yMax; y++) {
                    var coords = new int2(x, y);
                    GetChunkUnchecked(coords).BeginDeferredCoordsChanging(coords);
                }
            }
            
            foreach (var npc in World.All<NpcElement>()) {
                if (TryGetChunk(npc.Coords, out var chunk) && chunk.CoordsChangingData.isChangingPosition) {
                    chunk.AddNpc(npc);
                }
            }
            foreach (var alive in World.All<AliveLocation>()) {
                if (TryGetChunk(alive.Coords, out var chunk) && chunk.CoordsChangingData.isChangingPosition) {
                    chunk.AddAliveLocation(alive);
                }
            }
            foreach (var corpse in World.All<Corpse>()) {
                if (TryGetChunk(corpse.Coords, out var chunk) && chunk.CoordsChangingData.isChangingPosition) {
                    chunk.AddCorpse(corpse);
                }
            }
            foreach (var dummy in World.All<NpcDummy>()) {
                if (TryGetChunk(dummy.Coords, out var chunk) && chunk.CoordsChangingData.isChangingPosition) {
                    chunk.AddNpcDummy(dummy);
                }
            }
            
            for (int i = 0; i < _chunks.Length; i++) {
                _chunks[i].FinishDeferredCoordsChanging();
            }
            
            for (int x = xMin; x <= xMax; x++) {
                GetChunkUnchecked(new int2(x, yMin)).ResetDangerSpread();
                GetChunkUnchecked(new int2(x, yMax)).ResetDangerSpread();
            }
            for (int y = yMin; y <= yMax; y++) {
                GetChunkUnchecked(new int2(xMin, y)).ResetDangerSpread();
                GetChunkUnchecked(new int2(xMax, y)).ResetDangerSpread();
            }
        }

        // === Helpers

        public int2 GetCoordsOf(Vector3 position) => new(
            (int)math.floor(position.x / ChunkSize),
            (int)math.floor(position.z / ChunkSize)
        );

        public int2 GetCoordsOf(float2 position) => new(
            (int)math.floor(position.x / ChunkSize),
            (int)math.floor(position.y / ChunkSize)
        );

        public int2 GetCoordsOfWithHysteresis(int2 current, Vector3 position) {
            var continuousX = position.x / ChunkSize;
            var continuousY = position.z / ChunkSize;
            var discreteX = math.floor(continuousX);
            var discreteY = math.floor(continuousY);
            var x = (int)discreteX;
            var y = (int)discreteY;
            
            var continuousCenterX = current.x + 0.5f;
            var continuousDistanceX = math.abs(continuousCenterX - continuousX);
            var isCloseX = continuousDistanceX < _continuousHysteresis;
            x = math.select(x, current.x, isCloseX);
            
            var continuousCenterY = current.y + 0.5f;
            var continuousDistanceY = math.abs(continuousCenterY - continuousY);
            var isCloseY = continuousDistanceY < _continuousHysteresis;
            y = math.select(y, current.y, isCloseY);
            
            return new int2(x, y);
        }
        
        // === Querying
        
        public NpcGridSphereEnumerable<NpcElement, NpcChunk> GetNpcsInSphere(Vector3 center, float radius) => new(this, _chunks, center, radius);
        public NpcGridSphereEnumerable<AliveLocation, NpcChunk> GetAliveLocationsInSphere(Vector3 center, float radius) => new(this, _chunks, center, radius);
        public NpcGridSphereEnumerable<NpcDummy, NpcChunk> GetNpcDummiesInSphere(Vector3 center, float radius) => new(this, _chunks, center, radius);
        public NpcGridConeEnumerable<NpcElement, NpcChunk> GetNpcsInCone(Vector3 center, Vector3 forward, float radius, float cos, float sin) => new(this, _chunks, center, forward, radius, cos, sin);
        public NpcGridConeEnumerable<NpcElement, NpcChunk> GetNpcsInCone(Vector3 center, Vector3 forward, float radius, float radianAngle) => GetNpcsInCone(center, forward, radius, math.cos(radianAngle), math.sin(radianAngle));
        public NpcGridConeEnumerable<AliveLocation, NpcChunk> GetAliveLocationsInCone(Vector3 center, Vector3 forward, float radius, float cos, float sin) => new(this, _chunks, center, forward, radius, cos, sin);
        public NpcGridConeEnumerable<Corpse, NpcChunk> GetCorpsesInCone(Vector3 center, Vector3 forward, float radius, float cos, float sin) => new(this, _chunks, center, forward, radius, cos, sin);
        public NpcGridConeEnumerable<NpcDummy, NpcChunk> GetNpcDummiesInCone(Vector3 center, Vector3 forward, float radius, float radianAngle) => new(this, _chunks, center, forward, radius, math.cos(radianAngle), math.sin(radianAngle));
        [UnityEngine.Scripting.Preserve]
        public NpcGridConeEnumerable<NpcDummy, NpcChunk> GetNpcDummiesInCone(Vector3 center, Vector3 forward, float radius, float cos, float sin) => new(this, _chunks, center, forward, radius, cos, sin);
        public NpcGridHearingEnumerable GetHearingNpcs(Vector3 center, float radius) => new(this, center, radius);
        public NpcGridNotificationEnumerable GetNotifiedNpcs(NpcAI notifier, float coreRadius, float maxRadius) => new(this, notifier, coreRadius, maxRadius);

        [UnityEngine.Scripting.Preserve]
        public NpcGridChunkSquareEnumerable GetChunksInSquare(int xMin, int xMax, int yMin, int yMax) => new(this, xMin, xMax, yMin, yMax);
        public NpcGridChunkProximityEnumerable GetChunksByProximity(int2 center, int distance) => new(this, center, distance);
        
        // === Editor

        public readonly struct EDITOR_Accessor {
            readonly NpcGrid _grid;

            public EDITOR_Accessor(NpcGrid grid) => _grid = grid;

            public int2 Center => _grid._center;
            public NpcChunk GetChunkUnchecked(int x, int y) => _grid.GetChunkUnchecked(new int2(x, y));
        }
    }
}
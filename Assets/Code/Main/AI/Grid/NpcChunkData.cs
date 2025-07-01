using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Fights;

namespace Awaken.TG.Main.AI.Grid {
    public struct NpcChunkData {
        bool _hasDangerCombat;
        bool _hasHero;

        bool _hasLocalDanger;
        bool _hasLocalDangerForFearfuls;
        bool _hasSpreadDanger;
        bool _hasSpreadDangerForFearfuls;

        float _dangerEventLifetime;
        float _dangerCombatLifetime;
        float _dangerHeroLifetime;

        public readonly bool HasDanger => _hasLocalDanger || _hasSpreadDanger;
        public readonly bool HasDangerForFearfuls => _hasLocalDangerForFearfuls || _hasSpreadDangerForFearfuls;

        readonly bool HasHeroDanger => _hasHero && NpcDangerTracker.FleeingFromHero > 0;

        public void RefreshCombatDanger(NpcChunk chunk) {
            _hasDangerCombat = false;
            foreach (var npc in chunk.Npcs) {
                if (npc.IsInCombat()) {
                    _hasDangerCombat = true;
                    return;
                }
            }
        }
        
        public void UpdateLocalDanger(float deltaTime) {
            _hasLocalDanger = false;
            _hasLocalDanger |= NpcDangerTracker.UpdateDanger(ref _dangerEventLifetime, false, deltaTime, NpcDangerTracker.EventDangerLifetime);
            _hasLocalDanger |= NpcDangerTracker.UpdateDanger(ref _dangerCombatLifetime, _hasDangerCombat, deltaTime, NpcDangerTracker.CombatDangerLifetime);
            _hasLocalDanger |= NpcDangerTracker.UpdateDanger(ref _dangerHeroLifetime, HasHeroDanger, deltaTime, NpcDangerTracker.HeroDangerLifetime);
            if (!_hasLocalDanger) {
                _hasLocalDangerForFearfuls = false;
            }
        }

        public void UpdateSpreadDanger(in NpcChunk.Neighbours neighbours) {
            _hasSpreadDanger = false;
            _hasSpreadDanger |= neighbours.neighbour0.Data._hasLocalDanger;
            _hasSpreadDanger |= neighbours.neighbour1.Data._hasLocalDanger;
            _hasSpreadDanger |= neighbours.neighbour2.Data._hasLocalDanger;
            _hasSpreadDanger |= neighbours.neighbour3.Data._hasLocalDanger;
            _hasSpreadDanger |= neighbours.neighbour4.Data._hasLocalDanger;
            _hasSpreadDanger |= neighbours.neighbour5.Data._hasLocalDanger;
            _hasSpreadDanger |= neighbours.neighbour6.Data._hasLocalDanger;
            _hasSpreadDanger |= neighbours.neighbour7.Data._hasLocalDanger;
            
            _hasSpreadDangerForFearfuls = false;
            if (!_hasSpreadDanger) {
                return;
            }
            _hasSpreadDangerForFearfuls |= neighbours.neighbour0.Data._hasLocalDangerForFearfuls;
            _hasSpreadDangerForFearfuls |= neighbours.neighbour1.Data._hasLocalDangerForFearfuls;
            _hasSpreadDangerForFearfuls |= neighbours.neighbour2.Data._hasLocalDangerForFearfuls;
            _hasSpreadDangerForFearfuls |= neighbours.neighbour3.Data._hasLocalDangerForFearfuls;
            _hasSpreadDangerForFearfuls |= neighbours.neighbour4.Data._hasLocalDangerForFearfuls;
            _hasSpreadDangerForFearfuls |= neighbours.neighbour5.Data._hasLocalDangerForFearfuls;
            _hasSpreadDangerForFearfuls |= neighbours.neighbour6.Data._hasLocalDangerForFearfuls;
            _hasSpreadDangerForFearfuls |= neighbours.neighbour7.Data._hasLocalDangerForFearfuls;
        }

        public void ResetDangerSpread() {
            _hasSpreadDanger = false;
        }

        public void NotifyDangerousEvent(bool isDangerToFearfuls) {
            _dangerEventLifetime = NpcDangerTracker.EventDangerLifetime;
            _hasLocalDangerForFearfuls = _hasLocalDangerForFearfuls || isDangerToFearfuls;
        }

        public void SetHasHero(bool hasHero) {
            _hasHero = hasHero;
        }

        public void Clear() {
            _hasDangerCombat = false;
            _hasHero = false;
            _hasLocalDanger = false;
            _hasLocalDangerForFearfuls = false;
            _hasSpreadDanger = false;
            _hasSpreadDangerForFearfuls = false;
            _dangerEventLifetime = 0;
            _dangerCombatLifetime = 0;
            _dangerHeroLifetime = 0;
        }

        #if UNITY_EDITOR
        public struct EDITOR_Accessor {
            public float DangerEventLifetime(in NpcChunkData data) => data._dangerEventLifetime;
            public float DangerCombatLifetime(in NpcChunkData data) => data._dangerCombatLifetime;
            public float DangerHeroLifetime(in NpcChunkData data) => data._dangerHeroLifetime;
            public bool HasHero(in NpcChunkData data) => data._hasHero;
            public bool HasLocalDanger(in NpcChunkData data) => data._hasLocalDanger;
            public bool HasLocalDangerForFearfuls(in NpcChunkData data) => data._hasLocalDangerForFearfuls;
            public bool HasSpreadDanger(in NpcChunkData data) => data._hasSpreadDanger;
            public bool HasSpreadlDangerForFearfuls(in NpcChunkData data) => data._hasSpreadDangerForFearfuls;
        }
        #endif
    }
}
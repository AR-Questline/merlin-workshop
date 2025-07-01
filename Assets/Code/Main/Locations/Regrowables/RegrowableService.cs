using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Locations.Regrowables {
    public partial class RegrowableService : SerializedService, IDomainBoundService {
        public override ushort TypeForSerialization => SavedServices.RegrowableService;
        public Domain Domain => Domain.CurrentMainScene();
        public bool RemoveOnDomainChange() => true;

        // These list are sorted by spawn date (earlier date first)
        [Saved] List<RegrowData> _regrowData = new(300);
        // Index to first item which is not ready to spawn
        [Saved] int _head;

        public void Init(GameRealTime gameRealTime) {
            gameRealTime.ListenTo(GameRealTime.Events.GameTimeChanged, OnGameTimeChanged);
        }

        // === Queries
        public bool IsRegrowing(Regrowable regrowable) {
            var regrowableId = regrowable.Id;
            var count = _regrowData.Count;
            for (var i = 0; i < count; i++) {
                if (_regrowData[i].id == regrowableId) {
                    return true;
                }
            }

            return false;
        }

        // === Operations
        public void Register(Regrowable regrowable) {
            var gameRealTime = World.Only<GameRealTime>();
            var regrowTime = gameRealTime.WeatherTime + regrowable.RegrowRate;
#if UNITY_EDITOR || AR_DEBUG
            if (IsRegrowing(regrowable)) {
                Log.Important?.Error($"Cannot register regrowable {regrowable} because it is already registered");
            }
#endif
            // Insert into such spot, that lists are sorted after
            int insertIndex = FindInsertIndex(regrowTime);
            var regrowData = new RegrowData(regrowable.Id, regrowTime);
            _regrowData.Insert(insertIndex, regrowData);
        }

        void OnGameTimeChanged(ARDateTime weatherTime) {
            UpdateHead(weatherTime);
            TrySpawn();
        }

        void UpdateHead(ARDateTime currentTime) {
            while (_head < _regrowData.Count && currentTime >= _regrowData[_head].regrowTime) {
                ++_head;
            }
        }

        void TrySpawn() {
            for (int i = _head - 1; i >= 0; i--) {
                if (Regrowable.TryGetById(_regrowData[i].id, out var regrowable)) {
                    if (regrowable.TryRegrow()) {
                        RemoveAt(i);
                    }
                } else {
                    RemoveAt(i);
                }
            }
        }

        void RemoveAt(int index) {
            _regrowData.RemoveAt(index);
            if (index < _head) {
                --_head;
            }
        }
        
        int FindInsertIndex(ARDateTime regrowTime) {
            // Go from back to front while regrowTime is smaller that previous element time
            var insertIndex = _regrowData.Count;
            while (!CanInsert(insertIndex, regrowTime)) {
                --insertIndex;
            }
            return insertIndex;
        }
        
        bool CanInsert(int insertIndex, ARDateTime regrowTime) {
            var prevIndex = insertIndex - 1;
            if (prevIndex < 0) {
                return true;
            }
            return _regrowData[prevIndex].regrowTime <= regrowTime;
        }
        
        // === Helpers
        [Serializable]
        public partial struct RegrowData {
            public ushort TypeForSerialization => SavedTypes.RegrowData;

            [Saved] public SpecId id;
            [Saved] public ARDateTime regrowTime;

            public RegrowData(SpecId id, ARDateTime regrowTime) {
                this.id = id;
                this.regrowTime = regrowTime;
            }

            public override string ToString() {
                return $"RegrowData: {id} regrowTime: {regrowTime}";
            }
        }

        public override string ToString() {
            return $"RegrowableService: {_regrowData.Count} regrowables, head: {_head}";
        }
    }
}

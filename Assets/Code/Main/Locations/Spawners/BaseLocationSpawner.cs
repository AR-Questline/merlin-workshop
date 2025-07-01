using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Pathfinding;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.TG.Main.Locations.Spawners {
    public abstract partial class BaseLocationSpawner : Element<Location>, ICullingSystemRegistreeModel {
        const float MaxDistanceToInterruptRestSqr = 50f * 50f;
        const int SpawnFrameStagger = 5;

        static readonly UniversalProfilerMarker SpawnCostMarker = new("VLocationSpawner: Location Spawn Cost");
        static readonly EnumerableCache<SpawnedLocation> SpawnedLocationsCache = new(16);
        
        // === Fields
        [Saved] double lastSpawnInstance = float.NegativeInfinity;
        [Saved] double? lastClearOfGroup;

        // For faster saving/loading we need to pack this into single value, so to have some locality flags get/set are kept near
        [Saved] protected LocationSpawnerFlags _flags;
        protected bool DiscardAfterSpawn {
            get => _flags.HasFlagFast(LocationSpawnerFlags.DiscardAfterSpawn);
            set {
                if (value) {
                    _flags |= LocationSpawnerFlags.DiscardAfterSpawn;
                } else {
                    _flags &= ~LocationSpawnerFlags.DiscardAfterSpawn;
                }
            }
        }
        protected bool DiscardAfterAllKilled {
            get => _flags.HasFlagFast(LocationSpawnerFlags.DiscardAfterAllKilled);
            set {
                if (value) {
                    _flags |= LocationSpawnerFlags.DiscardAfterAllKilled;
                } else {
                    _flags &= ~LocationSpawnerFlags.DiscardAfterAllKilled;
                }
            }
        }
        protected bool SpawnOnlyAtNight {
            get => _flags.HasFlagFast(LocationSpawnerFlags.SpawnOnlyAtNight);
            set {
                if (value) {
                    _flags |= LocationSpawnerFlags.SpawnOnlyAtNight;
                } else {
                    _flags &= ~LocationSpawnerFlags.SpawnOnlyAtNight;
                }
            }
        }
        protected bool IsDisabledByFlag {
            get => _flags.HasFlagFast(LocationSpawnerFlags.IsDisabledByFlag);
            set {
                if (value) {
                    _flags |= LocationSpawnerFlags.IsDisabledByFlag;
                } else {
                    _flags &= ~LocationSpawnerFlags.IsDisabledByFlag;
                }
            }
        }
        protected bool DiscardSpawnedLocationsOnDiscard {
            get => _flags.HasFlagFast(LocationSpawnerFlags.DiscardSpawnedLocationsOnDiscard);
            set {
                if (value) {
                    _flags |= LocationSpawnerFlags.DiscardSpawnedLocationsOnDiscard;
                } else {
                    _flags &= ~LocationSpawnerFlags.DiscardSpawnedLocationsOnDiscard;
                }
            }
        }
        bool IsNight {
            get => _flags.HasFlagFast(LocationSpawnerFlags.IsNight);
            set {
                if (value) {
                    _flags |= LocationSpawnerFlags.IsNight;
                } else {
                    _flags &= ~LocationSpawnerFlags.IsNight;
                }
            }
        }
        bool SpawnAtFirstValidState {
            get => _flags.HasFlagFast(LocationSpawnerFlags.SpawnAtFirstValidState);
            set {
                if (value) {
                    _flags |= LocationSpawnerFlags.SpawnAtFirstValidState;
                } else {
                    _flags &= ~LocationSpawnerFlags.SpawnAtFirstValidState;
                }
            }
        }

        [Saved] protected StoryBookmark _storyOnAllKilled;
        [Saved] protected List<SpawnedLocation> spawnedLocations = new();
        [Saved] protected List<WeakModelRef<Location>> spawnedAliveLocations = new();
        [Saved] protected List<WeakModelRef<Location>> spawnedWyrdSpawns = new();
        [Saved] protected List<int> killedLocations = new();

        List<SpawnedLocation> _locationsToRestore;
        List<WeakModelRef<Location>> _aliveLocationsToRestore;

        Action<Location> _onLocationSpawned;

        protected FlagLogic _availability;
        protected int _batchQuantityToSpawn;
        int _currentBatchQuantitySpawned;
        protected bool _canTriggerAmbush;
        protected bool _spawnOnlyOnAmbush;
        protected bool _partialCanSpawnWyrdSpawns;
        protected bool _isManualSpawner;

        bool _wasAwayEnough = true;
        bool _spawnersDistanceIgnore;
        bool _isSpawning;
        bool _inSpawnBand;
        bool _isBatchSpawning;
        bool _isSpawningWyrdSpawns;

        // --- Movement
        float _lastMovementUpdate;
        float _previousTValue;

        // === Properties
        public Vector3 Coords => ParentModel.Coords;
        public Quaternion Rotation => ParentModel.Rotation;
        public bool IsSpawningWyrdSpawns => _isSpawningWyrdSpawns;
        bool CanAutomaticallySpawn => !_isManualSpawner && !_spawnOnlyOnAmbush && CanSpawn;
        bool CanSpawnAmbush => !_isManualSpawner && _canTriggerAmbush && CanSpawn;
        bool CanSpawn => !_isBatchSpawning && DistanceCondition && TimeOfDayCondition && !IsDisabledByFlag && ShouldSpawn() && ParentModel.Interactable;

        public float SpawnCooldown { get; protected set; } = 0.5f;
        public float SpawnCooldownAfterKilled { get; protected set; } = 120;
        public bool IsSpawning {
            get => _isSpawning;
            set {
                if (!value && _isSpawning) {
                    _wasAwayEnough = false;
                }
                _isSpawning = value;
            }
        }
        
        protected int CurrentlySpawned => spawnedLocations.Count + (_isSpawningWyrdSpawns ? spawnedWyrdSpawns.Count : 0);

        bool IsValidState => CurrentDomain == Domain.CurrentScene();
        bool DistanceCondition => (_inSpawnBand || SpawnersDistanceIgnore) && _wasAwayEnough;
        bool DistanceConditionWithoutIgnore => _inSpawnBand && _wasAwayEnough;
        bool TimeOfDayCondition => !SpawnOnlyAtNight || GameRealTime.WeatherTime.IsNight;
        bool CooldownCondition => !IsOnCooldown(GameRealTime.PlayRealTime.TotalSeconds);
        bool CanSpawnWyrdSpawns => _partialCanSpawnWyrdSpawns && CurrentlySpawned == 0 && DistanceConditionWithoutIgnore;
        
        bool DiscardAllSpawnedCondition => IsDisabledByFlag || (SpawnOnlyAtNight && !GameRealTime.WeatherTime.IsNight);
        
        bool SpawnersDistanceIgnore {
            get {
                if (_spawnersDistanceIgnore) {
                    _spawnersDistanceIgnore = false;
                    return true;
                }
                return false;
            }
            set {
                if (!LoadingScreenUI.IsFullyLoadingOrCreatingNewGame) {
                    _spawnersDistanceIgnore = value;
                    return;
                }
                _spawnersDistanceIgnore = false;
            }
        }

        protected abstract IEnumerable<LocationTemplate> AllUniqueTemplates { get; }
        bool IsHostileToHero => AllUniqueTemplates.Any(t => t.IsHostile(Hero.Current.Faction));
        
        GameRealTime _gameRealTime;
        GameRealTime GameRealTime => _gameRealTime ??= World.Only<GameRealTime>();
        
        // === Initialization
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        protected BaseLocationSpawner() { }
        
        protected override void OnInitialize() {
            lastClearOfGroup = float.NegativeInfinity;
            BaseInit();
        }

        protected override void OnRestore() {
            BaseInit();
            _locationsToRestore = new List<SpawnedLocation>(spawnedLocations);
            _aliveLocationsToRestore = new List<WeakModelRef<Location>>(spawnedAliveLocations);
            World.EventSystem.LimitedListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this,
                SceneInitializationEndedCallback, 1);
        }

        void BaseInit() {
            ModelUtils.DoForFirstModelOfType<Hero>(AssignHeroController, this);
            SpawnersDistanceIgnore = true;
            var gameRealTime = World.Only<GameRealTime>();
            gameRealTime.NightChanged += OnNightChange;

            // During init we use this flag to see whether we should listen to the flag change event
            if (IsDisabledByFlag) {
                IsDisabledByFlag = !_availability.Get();
                World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.FlagChanged, this, f => {
                    if (_availability.Flag == f) {
                        IsDisabledByFlag = !_availability.Get();
                    }
                });
            }
            
            if (_isManualSpawner) {
                AddElement<ManualSpawner>();
            }
        }

        void OnNightChange(bool isNight) {
            IsNight = isNight;
            if (!isNight) {
                return;
            }

            if (World.Services.Get<WyrdnessService>().IsInRepeller(Coords)) {
                // Spawner inside wyrd repeller, no wyrdspawns here
                return;
            }
            
            var mainScene = Services.Get<SceneService>().MainSceneRef;
            
            if (CurrentDomain != Domain.Scene(mainScene)) {
                // Spawner on an additive scene, no wyrdspawns here
                return;
            }

            bool spawnerInOpenWorld = CommonReferences.Get.SceneConfigs.IsOpenWorld(mainScene);
            
            if (spawnerInOpenWorld && CanSpawnWyrdSpawns) {
                // It's in the open world and doesn't restrict wyrdspawns, let's go!
                _isSpawningWyrdSpawns = true;
                SpawnPrefab().Forget();
            }
        }

        void SceneInitializationEndedCallback(SceneLifetimeEvents _) {
            bool anyLocationDiscardedOrKilled = false;
            bool anyAliveLocations = spawnedAliveLocations.Count > 0;
            
            foreach (var spawnedLocation in _locationsToRestore) {
                Location location = spawnedLocation.location.Get();
                if (location != null) {
                    location.ListenTo(Events.AfterDiscarded, AfterLocationDiscarded, this);
                } else {
                    spawnedLocations.Remove(spawnedLocation);
                    _aliveLocationsToRestore.Remove(spawnedLocation.location);
                    spawnedAliveLocations.Remove(spawnedLocation.location);
                    anyLocationDiscardedOrKilled = true;
                }
            }

            foreach (var aliveLocationRef in _aliveLocationsToRestore) {
                Location aliveLoc = aliveLocationRef.Get();
                if (aliveLoc != null && aliveLoc.TryGetElement(out IAlive alive)) {
                    var loc = spawnedLocations.FirstOrDefault(l => l.location.Get() == aliveLoc);
                    alive.ListenTo(IAlive.Events.BeforeDeath, d => AfterLocationKilled(loc.id, aliveLoc, d), this);
                } else {
                    spawnedAliveLocations.Remove(aliveLocationRef);
                    anyLocationDiscardedOrKilled = true;
                }
            }

            _locationsToRestore = null;
            _aliveLocationsToRestore = null;

            if (anyAliveLocations && spawnedAliveLocations.IsEmpty()) {
                OnAllAliveLocationKilled();
                if (HasBeenDiscarded) {
                    return;
                }
            }
            
            if (anyLocationDiscardedOrKilled) {
                OnLocationDiscardedOrKilled();
            }
            
            bool isWeatherNight = World.Only<GameRealTime>().WeatherTime.IsNight;
            if (isWeatherNight != IsNight) {
                OnNightChange(isWeatherNight);
            }
        }

        void AssignHeroController(Hero hero) {
            World.SpawnView<VLocationSpawner>(this, true);
        }

        // === Public
        public int CurrentlySpawnedByID(int id) => spawnedLocations.Count(l => l.id == id);
        public int KilledLocationCount(int id) => killedLocations.Count(l => l == id);
        [UnityEngine.Scripting.Preserve] public bool WasLocationKilled(int id) => killedLocations.Contains(id);
        public bool IsOnCooldown(double timeSinceLevelLoad) {
            if (lastClearOfGroup == null) return true;
            return timeSinceLevelLoad <= lastSpawnInstance + SpawnCooldown
                   || timeSinceLevelLoad <= lastClearOfGroup + SpawnCooldownAfterKilled;
        }
        
        public LocationTemplate WyrdSpawnTemplate() {
            foreach (var wyrdSpawn in GameConstants.Get.wyrdspawnVariants) {
                if (wyrdSpawn.maxHeroLevel >= Hero.Current.Level) {
                    return wyrdSpawn.enemyVariant.Get<LocationTemplate>();
                }
            }
            
            return GameConstants.Get.wyrdspawnVariants.Last().enemyVariant.Get<LocationTemplate>();
        }
        
        public void DiscardAllSpawnedLocations() {
            foreach (var location in SpawnedLocationsCache[spawnedLocations]) {
                location.location.Get()?.Discard();
            }

            lastClearOfGroup = float.NegativeInfinity;
        }
        
        public async UniTaskVoid SpawnPrefab() {
            if (!IsValidState) {
                SpawnAtFirstValidState = true;
                return;
            }
            SpawnAtFirstValidState = false;
            DiscardAllWyrdSpawns();
            _isBatchSpawning = true;
            await SpawnBatchStaggered();
            _isSpawningWyrdSpawns = false;
        }
        
        async UniTaskVoid SpawnPrefabIsValidState() {
            SpawnAtFirstValidState = false;
            DiscardAllWyrdSpawns();
            _isBatchSpawning = true;
            await SpawnBatchStaggered();
            _isSpawningWyrdSpawns = false;
        }

        async UniTask SpawnBatchStaggered() {
            if (_batchQuantityToSpawn <= 0) Log.Important?.Error("Unexpected batch quantity!");
            
            while (_currentBatchQuantitySpawned < _batchQuantityToSpawn && ShouldSpawn()) {
                SpawnCostMarker.Begin();
                SpawnPrefabInternal(_currentBatchQuantitySpawned);
                SpawnCostMarker.End();
                _currentBatchQuantitySpawned++;
                if (!await AsyncUtil.DelayFrame(this, SpawnFrameStagger)) return;
            }

            _onLocationSpawned = null;
            
            lastSpawnInstance = World.Only<GameRealTime>().PlayRealTime.TotalSeconds;
            _currentBatchQuantitySpawned = 0;
            _isBatchSpawning = false;
            
            if (!ShouldSpawn() && !_isSpawningWyrdSpawns) {
                if (DiscardAfterSpawn && !DiscardSpawnedLocationsOnDiscard) {
                    ParentModel.Discard();
                } else {
                    lastClearOfGroup = null;
                    IsSpawning = false;
                }
            }
        }

        public void DiscardAllWyrdSpawns() {
            foreach (var wyrdSpawn in spawnedWyrdSpawns) {
                wyrdSpawn.Get()?.Discard();
            }
            spawnedWyrdSpawns.Clear();
        }

        public void InterruptTimeSkipCheck(GameRealTime.TimeSkipData timeSkipData, ref bool prevented) {
            // Spawners are commonly set up to be triggered after 2 realtime hours (7200 seconds). It currently means 96 in game hours.
            const float RealTimeBonusMultiplierMin = 1f; // 1h wait: (3600s / 48) * 1 = 75s, 24h wait: 75s * 24 = 1800s. Min Wait Time to always trigger: 7200s / 75s = 96h
            const float RealTimeBonusMultiplierMax = 7f; // 1h wait: (3600s / 48) * 7 = 525s, 24h wait: 525s * 24 = 12600s. Min Wait Time that can trigger: 7200s / 525s = 13.7h
            // Waiting 24 hours after clearing a spawner should have a chance to trigger a spawner.
            // 7200s / 24 = 300, 300 / (3600s / 48) = 4, (4-1) / (7 - 1) = 0.5 chance to trigger.

            SpawnersDistanceIgnore = true;
            _wasAwayEnough = true;

            if (!CanSpawnAmbush) {
                return;
            }

            bool inInterruptDistance = ParentModel.Coords.SquaredDistanceTo(Hero.Current.Coords) < MaxDistanceToInterruptRestSqr;
            if (timeSkipData.safelySkipping && inInterruptDistance) {
                return;
            }

            var currentPlayTime = GameRealTime.PlayRealTime.TotalSeconds;
            var playTimeBonus = timeSkipData.timeSkippedInMinutes * 60f / GameRealTime.WeatherSecondsPerRealSecond;
            playTimeBonus *= RandomUtil.UniformFloat(RealTimeBonusMultiplierMin, RealTimeBonusMultiplierMax);

            // Ambushes while skipping time doesn't change current play time value.
            // It means that if the spawner is not triggered the realtime wait time stays the same.
            // It also means waiting multiple times is safer than waiting for longer.
            if (!IsOnCooldown(currentPlayTime + playTimeBonus)) {
                IsSpawning = true;
                SpawnPrefab().Forget();
                if (IsHostileToHero && inInterruptDistance) {
                    _onLocationSpawned += l => {
                        var npc = l.TryGetElement<NpcElement>();
                        npc?.OnCompletelyInitialized(static npc => npc.NpcAI.EnterCombatWith(Hero.Current));
                    };
                    prevented = true;
                }
            }
        }

        public void AfterTimeSkipped(GameRealTime.TimeSkipData data) {
            SpawnersDistanceIgnore = !data.safelySkipping;
            _wasAwayEnough = true;
        }

        // === Abstract

        protected abstract bool ShouldSpawn();
        protected abstract void SpawnPrefabInternal(int currentBatchQuantitySpawned);

        // === Event Reactions
        
        void AfterLocationKilled(int id, Location location, DamageOutcome damageOutcome) {
            if (damageOutcome.Attacker != location.TryGetElement<ICharacter>()) {
                killedLocations.Add(id);
            }

            SpawnedLocation spawnedLocation = spawnedLocations.FirstOrDefault(r => r.location.ID == location.ID);
            if (!spawnedLocation.location.IsSet) {
                Debug.LogException(new InvalidOperationException(
                    "AfterLocationKilled invoked on location that isn't known by BaseLocationSpawner."));
            } else {
                spawnedLocations.Remove(spawnedLocation);
            }

            if (spawnedAliveLocations.Remove(new(location)) && spawnedAliveLocations.IsEmpty()) {
                OnAllAliveLocationKilled();
                if (HasBeenDiscarded) {
                    return;
                }  
            }
            
            OnLocationDiscardedOrKilled();
        }

        void OnAllAliveLocationKilled() {
            if (_storyOnAllKilled is { IsValid: true }) {
                Story.StartStory(StoryConfig.Base(_storyOnAllKilled, typeof(VDialogue)));
            }
            if (DiscardAfterAllKilled) {
                ParentModel.Discard();
            }
        }
        
        protected void OnLocationSpawned(Location location, int id) {
            _onLocationSpawned?.Invoke(location);
            
            if (location.Template == WyrdSpawnTemplate()) {
                OnWyrdSpawnSpawned(location);
                return;
            }
            
            spawnedLocations.Add(new SpawnedLocation(location, id));
            location.ListenTo(Events.AfterDiscarded, AfterLocationDiscarded, this);
            
            var alive = location.TryGetElement<IAlive>();
            if (alive == null) {
                return;
            }
            spawnedAliveLocations.Add(location);
            alive.ListenTo(IAlive.Events.BeforeDeath, d => AfterLocationKilled(id, location, d), this);

            if (ParentModel.TryGetElement(out IdleDataElement idleDataElement)) {
                location.AddElement(new IdleDataOverride(idleDataElement));
            }
        }

        void OnWyrdSpawnSpawned(Location location) {
            spawnedWyrdSpawns.Add(location);
            location.AddElement(new DiscardParentIfNotInWyrdNight());
        }
        
        void AfterLocationDiscarded(Model model) {
            SpawnedLocation spawnedLocation = spawnedLocations.FirstOrDefault(r => r.location.ID == model.ID);
            spawnedLocations.Remove(spawnedLocation);
            OnLocationDiscardedOrKilled();
        }

        void OnLocationDiscardedOrKilled() {
            if (ShouldSpawn()) {
                lastClearOfGroup ??= World.Only<GameRealTime>().PlayRealTime.TotalSeconds;
            }
        }

        // === Checks

        public static Vector3 VerifyPosition(Vector3 position, LocationTemplate template, bool allowSnapToGround = true) {
            return VerifyPosition(position, template.GetComponent<NpcAttachment>() != null, allowSnapToGround);
        }
        public static Vector3 VerifyPosition(Vector3 position, bool isNpc, bool allowSnapToGround = true) {
            try {
                if (allowSnapToGround) {
                    position.y = Ground.HeightAt(position, findClosest: true, performExtraChecks: true);
                }
                if (isNpc) {
                    float originalMaxNearestNodeDistance = AstarPath.active.maxNearestNodeDistance;
                    AstarPath.active.maxNearestNodeDistance = 5f;
                    NNInfo nearest = AstarPath.active.GetNearest(position, NNConstraint.Walkable);
                    if (nearest.node != null) {
                        position = nearest.position;
                    }
                    AstarPath.active.maxNearestNodeDistance = originalMaxNearestNodeDistance;
                }
            } catch (Exception) {
                //Ignore
            }
            return position;
        }
        
        // === Moving spawner

        public void UnityUpdate() {
            if (SpawnAtFirstValidState && IsValidState) { 
                SpawnPrefabIsValidState().Forget();
                return;
            }
            
            if (CurrentlySpawned > 0 && DiscardAllSpawnedCondition) {
                DiscardAllSpawnedLocations();
            }

            if (CanAutomaticallySpawn && CooldownCondition) {
                IsSpawning = true;
                SpawnPrefab().Forget();
            }
        }
        
        public partial struct SpawnedLocation : IEquatable<SpawnedLocation> {
            public ushort TypeForSerialization => SavedTypes.SpawnedLocation;

            [Saved] public WeakModelRef<Location> location;
            [Saved] public int id;

            public SpawnedLocation(WeakModelRef<Location> location, int id) {
                this.location = location;
                this.id = id;
            }

            public bool Equals(SpawnedLocation other) {
                return id == other.id && location.Equals(other.location);
            }

            public override bool Equals(object obj) {
                return obj is SpawnedLocation other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    return (location.GetHashCode() * 397) ^ id;
                }
            }
        }
        
        // === Culling System
        Registree Registree { get; set; }

        public void CullingSystemBandUpdated(int newDistanceBand) {
            _inSpawnBand = LocationSpawnerCullingGroup.InSpawnBand(newDistanceBand);
            if (LocationSpawnerCullingGroup.InAwayEnoughBand(newDistanceBand)) {
                _wasAwayEnough = true;
            }
        }

        public Registree GetRegistree() {
            ParentModel.ListenTo(GroundedEvents.AfterTeleported, AfterSpawnerTeleport, this);
            Hero.Current.ListenTo(GroundedEvents.AfterTeleported, AfterHeroTeleport, this);
            return Registree = Registree.ConstructFor<LocationSpawnerCullingGroup>(this).Build();
        }

        void AfterSpawnerTeleport(IGrounded obj) {
            Registree?.UpdateOwnPosition();
        }

        void AfterHeroTeleport() {
            SpawnersDistanceIgnore = true;
        }
        
         // === Discarding
         protected override void OnDiscard(bool fromDomainDrop) {
             var gameTime = World.Any<GameRealTime>();
             if (gameTime) {
                 gameTime.NightChanged -= OnNightChange;
             }

             if (!fromDomainDrop && DiscardSpawnedLocationsOnDiscard) {
                 foreach (var s in SpawnedLocationsCache[spawnedLocations]) {
                     if (s.location.TryGet(out Location spawnedLocation)) {
                         spawnedLocation.Discard();
                     }
                 }
                 foreach (var s in spawnedWyrdSpawns) {
                     if (s.TryGet(out Location spawnedLocation)) {
                         spawnedLocation.Discard();
                     }
                 }
             }
             base.OnDiscard(fromDomainDrop);
         }

         [Flags]
         protected enum LocationSpawnerFlags : byte {
             DiscardAfterSpawn = 1 << 0,
             DiscardAfterAllKilled = 1 << 1,
             SpawnOnlyAtNight = 1 << 2,
             IsDisabledByFlag = 1 << 3,
             DiscardSpawnedLocationsOnDiscard = 1 << 4,
             IsNight = 1 << 5,
             SpawnAtFirstValidState = 1 << 6,
         }
    }
}

using System;
using System.Linq;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Tristan;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class Tristan : BaseBossCombat {
        public override ushort TypeForSerialization => SavedModels.Tristan;
        
        [SerializeField] WeaponsSet[] weaponsSetPerPhase = new WeaponsSet[2];
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] ShareableARAssetReference flyingVFX;

        [Title("Trident Throwing")] 
        [SerializeField] int attacksNeededToBeAbleToThrowTrident = 7;
        [SerializeField] int attacksNeededToBeAbleToPullBackTrident = 3;
        [SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference stuckTrident;

        [Title("Stalagmites and Water Wave")] 
        [SerializeField] int attackNeededToSpawnStalagmites = 4;
        [SerializeField] int attackNeededToPerformWaterWave = 3;
        [SerializeField] LocationReference waterWaveTargetPosition;
        [SerializeField] LocationReference stalagmiteSpawnPoints;
        [SerializeField] FloatRange fallDelay = new FloatRange(0.1f, 0.6f); 
        [SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference fellDownStalagmite;
        [SerializeField] float stalagmiteBlockingRadius = 2;
        [SerializeField] float maxBlockingDistanceBehind = 6;
        [SerializeField] float discardStalagmiteDelay = 2f;
        [SerializeField] float movementSpeedMultiplier = 1.7f;
        [SerializeField] float accelerationSpeedMultiplier = 1.5f;
        [SerializeField] float rotationSpeedMultiplier = 2f;

        public override bool UsesCombatMovementAnimations => true;
        public override bool UsesAlertMovementAnimations => false;
        public float StalagmiteRadius => stalagmiteBlockingRadius;
        public float MaxBlockingDistanceBehind => maxBlockingDistanceBehind;

        public override void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            Tristan copyFrom = (Tristan)spec.BossBaseClass;
            weaponsSetPerPhase = new WeaponsSet[weaponsSetPerPhase.Length];
            for (int i = 0; i < weaponsSetPerPhase.Length; i++) {
                weaponsSetPerPhase[i] = new WeaponsSet {
                    mainHand = new ItemSpawningData(copyFrom.weaponsSetPerPhase[i].mainHand.itemTemplateReference, copyFrom.weaponsSetPerPhase[i].mainHand.ItemLvl),
                    offHand = new ItemSpawningData(copyFrom.weaponsSetPerPhase[i].offHand.itemTemplateReference, copyFrom.weaponsSetPerPhase[i].offHand.ItemLvl)
                };
            }
            flyingVFX = copyFrom.flyingVFX;
            
            attacksNeededToBeAbleToThrowTrident = copyFrom.attacksNeededToBeAbleToThrowTrident;
            attacksNeededToBeAbleToPullBackTrident = copyFrom.attacksNeededToBeAbleToPullBackTrident;
            stuckTrident = copyFrom.stuckTrident;

            attackNeededToSpawnStalagmites = copyFrom.attackNeededToSpawnStalagmites;
            attackNeededToPerformWaterWave = copyFrom.attackNeededToPerformWaterWave;
            waterWaveTargetPosition = copyFrom.waterWaveTargetPosition;
            stalagmiteSpawnPoints = copyFrom.stalagmiteSpawnPoints;
            fallDelay = copyFrom.fallDelay;
            fellDownStalagmite = copyFrom.fellDownStalagmite;
            stalagmiteBlockingRadius = copyFrom.stalagmiteBlockingRadius;
            maxBlockingDistanceBehind = copyFrom.maxBlockingDistanceBehind;
            discardStalagmiteDelay = copyFrom.discardStalagmiteDelay;
            movementSpeedMultiplier = copyFrom.movementSpeedMultiplier;
            accelerationSpeedMultiplier = copyFrom.accelerationSpeedMultiplier;
            rotationSpeedMultiplier = copyFrom.rotationSpeedMultiplier;
            
            InitTrident();
            InitStalagmites();
            base.InitFromAttachment(spec, isRestored);
        }
        
        protected override void OnFullyInitialized() {
            NpcElement.OnCompletelyInitialized(_ => {
                NpcElement.ListenTo(IAlive.Events.BeforeDeath, OnBeforeDeath, this);
                EquipItemSet(0);
            });
            base.OnFullyInitialized();
        }
        
        protected override void OnBehaviourStarted(IBehaviourBase behaviour) {
            base.OnBehaviourStarted(behaviour);
            switch (behaviour) {
                case TristanThrowTrident:
                    _tridentPriorityMultiplier = TridentStartingPriorityPullBack;
                    return;
                case TristanPickUpTrident:
                    _tridentPriorityMultiplier = TridentStartingPriorityThrow;
                    _stalagmitesPriorityMultiplier = AnyStalagmites ? WaterWaveStartingPriority : StalagmitesStartingPriority;
                    return;
                case TristanFallingStalagmites:
                    _stalagmitesPriorityMultiplier = WaterWaveStartingPriority;
                    return;
                case TristanWaterWave:
                    _stalagmitesPriorityMultiplier = StalagmitesStartingPriority;
                    return;
            }
            if (!behaviour.IsPeaceful) {
                _tridentPriorityMultiplier += TridentPriorityGainPerBehaviourStart;
                _stalagmitesPriorityMultiplier += StalagmitesPriorityGainPerBehaviourStart;
            }
        }

        // === Phases
        
        Item _currentMainHand, _currentOffHand;
        CancellationTokenSource _flyingVFXCts;
        IPooledInstance _flyingVFXInstance;
        UntilDiscarded _angularSpeedMultiplierDuration;
        StatTweak _movementSpeedTweak;
        
        void EDITOR_NextPhase() {
            SetPhase((CurrentPhase + 1) % weaponsSetPerPhase.Length);
        }

        protected override void OnPhaseTransitionFinished(int phase) {
            ChangeItemSet(phase);
            if (phase == 1) {
                if (flyingVFX is { IsSet: true }) {
                    ShowFlyingVFX().Forget();
                }
                _angularSpeedMultiplierDuration = new UntilDiscarded();
                NpcAngularSpeedMultiplier.AddUnclampedAngularSpeedMultiplier(NpcElement, rotationSpeedMultiplier, _angularSpeedMultiplierDuration);
                var controller = NpcElement.Controller;
                controller.SetCustomAcceleration(controller.DefaultAccelerationSpeed * accelerationSpeedMultiplier, controller.DefaultDecelerationSpeed * accelerationSpeedMultiplier);
                _movementSpeedTweak = StatTweak.Multi(NpcElement.Stat(CharacterStatType.MovementSpeedMultiplier), movementSpeedMultiplier, parentModel: this);
            } else {
                VFXUtils.StopVfxAndReturn(_flyingVFXInstance, 5f);
                _flyingVFXInstance = null;
                _angularSpeedMultiplierDuration?.Discard();
                _angularSpeedMultiplierDuration = null;
                _movementSpeedTweak?.Discard();
                _movementSpeedTweak = null;
                NpcElement.Controller.ResetCustomAcceleration();
            }
        }

        async UniTaskVoid ShowFlyingVFX() { 
            _flyingVFXCts = new CancellationTokenSource();
            _flyingVFXInstance = await PrefabPool.Instantiate(flyingVFX, Vector3.zero, Quaternion.identity, NpcElement.Hips, cancellationToken: _flyingVFXCts.Token);
            if (_flyingVFXInstance.Instance == null) {
                _flyingVFXInstance.Release();
                _flyingVFXInstance = null;
            }
        }
        
        void ChangeItemSet(int phase) {
            EquipItemSet(phase);
            StartBehaviour(Element<EquipWeaponBehaviour>());
        }
        
        void EquipItemSet(int phase) {
            _currentMainHand ??= NpcElement.NpcItems.EquippedItem(EquipmentSlotType.MainHand);
            _currentOffHand ??= NpcElement.NpcItems.EquippedItem(EquipmentSlotType.OffHand);
            _currentMainHand?.Discard();
            _currentOffHand?.Discard();

            _currentMainHand = weaponsSetPerPhase[phase].mainHand.itemTemplateReference is { IsSet: true }
                ? new Item(weaponsSetPerPhase[phase].mainHand.ToRuntimeData(this))
                : null;
            _currentOffHand = weaponsSetPerPhase[phase].mainHand.itemTemplateReference is { IsSet: true }
                ? new Item(weaponsSetPerPhase[phase].mainHand.ToRuntimeData(this))
                : null;
            if (_currentMainHand != null) {
                NpcElement.NpcItems.Add(_currentMainHand);
                NpcElement.NpcItems.EquipItem(EquipmentSlotType.MainHand, _currentMainHand);
            }
            if (_currentOffHand != null) {
                NpcElement.NpcItems.Add(_currentOffHand);
                NpcElement.NpcItems.EquipItem(EquipmentSlotType.OffHand, _currentOffHand);
            }
        }
        
        void CleanupWeapons() {
            if (_currentMainHand is { HasBeenDiscarded: false }) {
                _currentMainHand.Discard();
                _currentMainHand = null;
            }
            if (_currentOffHand is { HasBeenDiscarded: false }) {
                _currentOffHand.Discard();
                _currentOffHand = null;
            }
        }

        void CleanupFlyingVFX() {
            _flyingVFXInstance?.Return();
            _flyingVFXInstance = null;
            _flyingVFXCts?.Cancel();
            _flyingVFXCts = null;
        }
        
        // Thrown Trident

        const float TridentPriorityGainPerBehaviourStart = 0.4f;
        const float TridentPickUpPriorityPerDistanceBonusMax = 1f;
        const float TridentPickUpPriorityPerDistanceBonusDistanceMaxSqr = 6f * 6f;
        const float TridentPickUpPriorityPerDistanceBonusDistanceMinSqr = 20f * 20f;

        float TridentStartingPriorityThrow => -1 * attacksNeededToBeAbleToThrowTrident * TridentPriorityGainPerBehaviourStart;
        float TridentStartingPriorityPullBack => -1 * attacksNeededToBeAbleToPullBackTrident * TridentPriorityGainPerBehaviourStart;
        
        WeakModelRef<Location> _stuckTrident;
        float _tridentPriorityMultiplier;
        
        public bool IsTridentWaiting => _stuckTrident.TryGet(out _);

        void InitTrident() {
            _tridentPriorityMultiplier = TridentStartingPriorityThrow;
        }

        public float GetTridentThrowPriorityMultiplier() {
            if (CurrentPhase != 0) {
                return 0;
            }
            return _tridentPriorityMultiplier;
        }

        public float GetTridentPickUpPriorityMultiplier() {
            if (CurrentPhase != 1 || !_stuckTrident.TryGet(out var trident)) {
                return 0;
            }
            float distanceSqr = (trident.Coords - NpcElement.Coords).sqrMagnitude;
            float distanceBonus;
            if (distanceSqr <= TridentPickUpPriorityPerDistanceBonusDistanceMaxSqr) {
                distanceBonus = TridentPickUpPriorityPerDistanceBonusMax;
            } else if (distanceSqr >= TridentPickUpPriorityPerDistanceBonusDistanceMinSqr) {
                distanceBonus = 0;
            } else {
                float lerpValue = (distanceSqr - TridentPickUpPriorityPerDistanceBonusDistanceMaxSqr) /
                                  (TridentPickUpPriorityPerDistanceBonusDistanceMinSqr - TridentPickUpPriorityPerDistanceBonusDistanceMaxSqr);
                distanceBonus = math.lerp(TridentPickUpPriorityPerDistanceBonusMax, 0, lerpValue);
            }
            return _tridentPriorityMultiplier + distanceBonus;
        }

        public async UniTaskVoid ShootTridentFromHand(CombatBehaviourUtils.FireProjectileParams fireParams, VGUtils.ShootParams shootParams, KnockdownType knockdownType, float knockdownStrength) {
            HideWeapons();
            var wrapper = CombatBehaviourUtils.FireProjectile(fireParams, shootParams);
            wrapper.SetKnockdownData(knockdownType, knockdownStrength);
            await wrapper.WaitForProjectileInstanceToLoad();
            SetPhase(1);
        }

        public async UniTaskVoid MoveTridentToHand(CombatBehaviourUtils.FireProjectileParams fireParams, VGUtils.ShootParams shootParams) {
            HideWeapons();
            shootParams.startPosition = _stuckTrident.Get().Coords;
            fireParams.target = NpcElement;
            var wrapper = CombatBehaviourUtils.FireProjectile(fireParams, shootParams);
            wrapper.HomingProjectileSetTarget(NpcElement);
            await wrapper.WaitForProjectileInstanceToLoad();
            CleanupTrident();
        }
        
        public void OnTridentProjectileDestroy(DamageDealingProjectile projectile, Vector3 position) {
            if (_stuckTrident is { IsSet: true }) {
                return;
            }
            Quaternion rot = Quaternion.Euler(0, projectile.transform.rotation.eulerAngles.y, 0);
            var trident = stuckTrident.Get<LocationTemplate>().SpawnLocation(position, rot);
            trident.MarkedNotSaved = true;
            trident.TryGetElement<PersistentAoE>()?.AssignDamageDealer(NpcElement);
            _stuckTrident = trident;
        }
        
        public void OnTridentReturnProjectileDestroy(DamageDealingProjectile projectile) {
            if (projectile is not { HasBeenDiscarded: false }) {
                return;
            }
            projectile.Discard();
            SetPhase(0);
        }

        void HideWeapons() {
            CleanupWeapons();
        }

        void CleanupTrident() {
            if (_stuckTrident.TryGet(out var trident)) {
                trident.Discard();
                _stuckTrident = null;
            }
        }
        
        // Stalagmites and Water Wave

        const float StalagmitesPriorityGainPerBehaviourStart = 0.4f;
        
        float _stalagmitesPriorityMultiplier;
        StructList<WeakModelRef<Location>> _stalagmites;
        
        public bool AnyStalagmites => _stalagmites is { IsCreated: true, Count: > 0 };
        public float StalagmitesPriorityMultiplier => _stalagmitesPriorityMultiplier;
        float StalagmitesStartingPriority => -1 * attackNeededToSpawnStalagmites * StalagmitesPriorityGainPerBehaviourStart;
        float WaterWaveStartingPriority => -1 * attackNeededToPerformWaterWave * StalagmitesPriorityGainPerBehaviourStart;

        void InitStalagmites() {
            _stalagmitesPriorityMultiplier = StalagmitesStartingPriority;
        }
        
        public async UniTaskVoid CreateFallingStalagmites(CombatBehaviourUtils.FireProjectileParams fireParams, VGUtils.ShootParams shootParams, KnockdownType knockdownType, float knockdownStrength) {
            _stalagmites = new StructList<WeakModelRef<Location>>(1);
            foreach (var spawnPoint in stalagmiteSpawnPoints.MatchingLocations(null)) {
                shootParams.startPosition = spawnPoint.Coords;
                fireParams.target = null;
                fireParams.shootPos = shootParams.startPosition + Vector3.down;
                var wrapper = CombatBehaviourUtils.FireProjectile(fireParams, shootParams);
                wrapper.SetKnockdownData(knockdownType, knockdownStrength);
                if (!await AsyncUtil.DelayTime(this, fallDelay.RandomPick())) {
                    return;
                }
            }
        }

        public async UniTask<bool> StalagmiteFell(DamageDealingProjectile projectile, Vector3 position) {
            Quaternion rot = Quaternion.Euler(0, projectile.transform.rotation.eulerAngles.y, 0);
            var stalagmite = fellDownStalagmite.Get<LocationTemplate>().SpawnLocation(position, rot);
            stalagmite.MarkedNotSaved = true;
            _stalagmites.Add(stalagmite);
            if (!await AsyncUtil.WaitUntil(projectile, () => stalagmite is not { HasBeenDiscarded: false, IsVisualLoaded: false })) {
                return false;
            }
            return true;
        }

        public void SpawnVFXAndDestroyStalagmites(ShareableARAssetReference vfx, float waveSpeed) {
            WaterWave.SpawnVfxAndDestroyBlockers(NpcElement, ref _stalagmites, vfx, NpcElement.Coords.ToVector2(), waveSpeed, true, discardStalagmiteDelay);
        }

        public Vector2[] GetAllStalagmitesPositions() {
            return WaterWave.GetAllBlockerPositions(ref _stalagmites);
        }

        public TeleportDestination GetWaterWaveTeleportDestination() {
            var targetLocation = waterWaveTargetPosition.MatchingLocations(null).First();
            return new TeleportDestination() {
                position = targetLocation.Coords,
                Rotation = targetLocation.Rotation
            };
        }
        
        void CleanupStalagmites() {
            foreach (var stalagmiteRef in _stalagmites) {
                if (stalagmiteRef.TryGet(out var stalagmite)) {
                    stalagmite.Discard();
                }
            }
        }
        
        // === LifeCycle

        protected void OnBeforeDeath() {
            CleanupWeapons();
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            CleanupWeapons();
            CleanupTrident();
            CleanupFlyingVFX();
            CleanupStalagmites();
            base.OnDiscard(fromDomainDrop);
        }

        [Serializable]
        public struct WeaponsSet {
            public ItemSpawningData mainHand;
            public ItemSpawningData offHand;
        }
    }
}

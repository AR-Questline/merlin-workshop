using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Combat.Attachments.Humanoids {
    public abstract partial class HumanoidCombatBaseClass<T> : HumanoidCombatBaseClass, IRefreshedByAttachment<T> where T : IAttachmentSpec {
        public abstract void InitFromAttachment(T spec, bool isRestored);
    }
    
    public abstract partial class HumanoidCombatBaseClass : EnemyBaseClass, IDesiredDistanceToTargetProvider, IMinDistanceToTargetProvider {
        const float AllowToSwitchWeaponsDelay = 10f;
        
        [Saved(false)] bool _weaponsEquipped;
        bool _hasRanged, _rangedEquipped;
        FighterType _currentFighterType;
        ItemProjectile _itemProjectile;
        ProjectileData _customProjectileData;
        float _lastMeleeWeaponEquipTimeStamp;

        public float DesiredDistanceToTarget { get; private set; }
        public float MinDistanceToTarget { get; private set; }
        public FighterType CurrentFighterType {
            get => _currentFighterType;
            private set {
                _currentFighterType = value;
                UpdateDistancesToTargets();
            }
        }
        public FloatRange MeleeRangedSwitchDistance { get; protected set; }
        public ItemProjectile ItemProjectile {
            get {
                if (_itemProjectile is { HasBeenDiscarded: false }) {
                    return _itemProjectile;
                }

                if (_customProjectileData.logicPrefab is { IsSet: true }) {
                    return null;
                }

                Item quiver = NpcElement.Inventory.EquippedItem(EquipmentSlotType.Quiver);
                _itemProjectile = quiver?.TryGetElement<ItemProjectile>();
                return _itemProjectile;
            }
        }

        public ProjectileData CustomProjectileData {
            get {
                if (_customProjectileData.logicPrefab is { IsSet: true} ) {
                    return _customProjectileData;
                }

                if (ArrowOverride is { logicPrefab: { IsSet: true } } arrowOverride) {
                    _customProjectileData = arrowOverride;
                    return _customProjectileData;
                }

                var commonRefs = Services.Get<CommonReferences>();
                _customProjectileData = new ProjectileData(commonRefs.ArrowLogicPrefab, commonRefs.ArrowPrefab, null, ProjectileLogicData.DefaultArrow);
                return _customProjectileData;
            }
        }
        
        
        protected virtual ProjectileData? ArrowOverride => null;

        bool CanEquipRanged {
            get {
                GuardIntervention guardIntervention = TryGetElement<GuardIntervention>();
                return guardIntervention is not { CanIntervene: true };
            }
        }

        bool InSwitchTimeout => Time.time - _lastMeleeWeaponEquipTimeStamp < AllowToSwitchWeaponsDelay;

        // === Initialization
        protected override void AfterVisualLoaded(Transform parentTransform) {
            UpdateDistancesToTargets();
            NpcElement.ListenTo(Events.AfterWeaponFullyLoaded, force => ChangeCombatData(force), this);
            NpcElement.Inventory.ListenTo(ICharacterInventory.Events.SlotChanged(EquipmentSlotType.Quiver), OnQuiverChanged, this);
        }

        protected override void AfterItemsAddedToInventory() {
            _hasRanged = NpcElement.Inventory.Items.Any(i => i.IsRanged);
        }

        public override void RefreshFightingStyle() {
            base.RefreshFightingStyle();
            SwitchWeapons(false, false, true);
        }
        
        public override void OnWyrdConversionStarted() {
            SwitchWeapons(false, false, true);
        }

        public void TrySwitchWeapons(bool forceMelee, bool forceRanged, bool ignoreAnimation) {
            if (!forceMelee && !forceRanged && _weaponsEquipped && InSwitchTimeout) {
                return;
            }
            SwitchWeapons(forceMelee, forceRanged, ignoreAnimation);
        }
        
        void SwitchWeapons(bool forceMelee, bool forceRanged, bool ignoreAnimation) {
            bool wasWeaponEquipped = _weaponsEquipped;
            EquipWeapons(true, out var equippedWeapons, forceMelee, forceRanged);

            bool currentBehaviourCanBeInterrupted = CurrentBehaviour.Get()?.CanBeInterrupted ?? true;
            ignoreAnimation |= WeaponsAlwaysEquipped || !currentBehaviourCanBeInterrupted;
            if (ignoreAnimation) {
                if (NpcElement.TryGetElement(out NpcWeaponsHandler weaponsHandler)) {
                    foreach (Item weapon in equippedWeapons) {
                        weaponsHandler.AttachWeaponToHand(weapon.Element<ItemEquip>());
                    }
                }
                return;
            }
            
            // --- If is already equipping weapon and we didn't changed weapon type return.
            if (CurrentBehaviour.Get() is EquipWeaponBehaviour equipWeapon && equipWeapon.EquippingMelee == !_rangedEquipped) {
                return;
            }
            
            if (TryGetElement(out EquipWeaponBehaviour equipWeaponBehaviour)) {
                equipWeaponBehaviour.SetSettings(!wasWeaponEquipped, !_rangedEquipped);
                StartBehaviour(equipWeaponBehaviour);
            }
        }

        protected override bool EquipWeapons(bool withResults, out List<Item> equippedItems, bool forceMelee = false, bool forceRanged = false) {
            if (CurrentBehaviour.Get() is UnEquipWeaponBehaviour unEquipWeaponBehaviour) {
                unEquipWeaponBehaviour.isExitingToCombat = true;
                StopCurrentBehaviour(false);
            }
            
            UpdateDistanceToTarget(ParentModel.GetDeltaTime(), NpcElement?.GetCurrentTarget());
            bool equipRanged = forceRanged || DistanceToTarget > MeleeRangedSwitchDistance.min;
            if (!forceMelee && equipRanged && CanEquipRanged && TryEquipRangedWeapon(forceRanged, out equippedItems)) {
                OnWeaponsEquipped();
            } else if (TryEquipMeleeWeapon(out equippedItems)) {
                _lastMeleeWeaponEquipTimeStamp = Time.time;
                OnWeaponsEquipped();
            } else {
                Log.Important?.Error($"Failed to equip weapons for: {ParentModel.Spec}!");
                return false;
            }
            return true;

            void OnWeaponsEquipped() {
                _weaponsEquipped = true;
                ChangeCombatData();
            }
        }

        bool TryEquipRangedWeapon(bool forceRanged, out List<Item> equippedItems) {
            Item rangedWeapon = null;
            Item arrows = null;
            foreach (var item in NpcElement.NpcItems.Items) {
                if (item.IsRanged && rangedWeapon == null) {
                    rangedWeapon = item;
                } else if (item.IsArrow && arrows == null) {
                    arrows = item;
                }

                if (rangedWeapon != null && arrows != null) {
                    break;
                }
            }

            if (rangedWeapon == null) {
                if (forceRanged) {
                    Log.Important?.Error($"No ranged weapon found for: {ParentModel.Spec}!");
                }

                equippedItems = null;
                return false;
            }

            equippedItems = new List<Item>(arrows != null ? 2 : 1);
            if (NpcElement.Inventory.Equip(rangedWeapon)) {
                equippedItems.Add(rangedWeapon);
            }

            if (arrows != null) {
                if (NpcElement.Inventory.Equip(arrows)) {
                    equippedItems.Add(arrows);
                }
            }

            _rangedEquipped = true;
            return true;
        }

        bool TryEquipMeleeWeapon(out List<Item> equippedItems) {
            bool success = base.EquipWeapons(true, out equippedItems, true, false);
            _rangedEquipped = _rangedEquipped && equippedItems.Count <= 0;
            return success;
        }

        protected override void UnEquipWeapons() {
            base.UnEquipWeapons();
            _weaponsEquipped = false;
            _rangedEquipped = false;
            UpdateFighterType(null).Forget();
        }
        
        // === LifeCycle
        protected override void NotInCombatUpdate(float deltaTime) {
            ToggleWeapons();
        }
        
        protected override void Tick(float deltaTime, NpcElement npc) {
            ToggleWeapons();
            
            if (npc.GetCurrentTarget() == null) {
                StopCurrentBehaviour(false);
                return;
            }

            if (CurrentBehaviour.Get() == null) {
                SelectNewBehaviour();
            }
        }

        void ToggleWeapons() {
            bool alertedOrInCombat = ShouldWeaponsBeEquipped;
            if (alertedOrInCombat && !NpcElement.IsUnconscious) {
                bool equipRangedWeapon = !_rangedEquipped && _hasRanged && CanEquipRanged && DistanceToTarget >= MeleeRangedSwitchDistance.max;
                bool equipMeleeWeapon = _rangedEquipped && DistanceToTarget <= MeleeRangedSwitchDistance.min;
                if (!_weaponsEquipped || equipRangedWeapon || equipMeleeWeapon) {
                    TrySwitchWeapons(false, false, false);
                }
            } else if (_weaponsEquipped && !alertedOrInCombat) {
                UnEquipWeapons();
            }
        }

        protected override void OnEnterCombat() {
            DistancesToTargetHandler.AddDesiredDistanceToTargetProvider(NpcElement, this);
            DistancesToTargetHandler.AddMinDistanceToTargetProvider(NpcElement, this);
        }

        protected override void OnExitCombat() {
            if (NpcElement is { HasBeenDiscarded: false }) {
                DistancesToTargetHandler.RemoveDesiredDistanceToTargetProvider(NpcElement, this);
                DistancesToTargetHandler.RemoveMinDistanceToTargetProvider(NpcElement, this);
            }

            base.OnExitCombat();
        }

        // === Helpers
        CancellationTokenSource _fighterTypeCancellationToken;
        
        async UniTaskVoid UpdateFighterType(FighterType fighterType) {
            _fighterTypeCancellationToken?.Cancel();
            _fighterTypeCancellationToken = new CancellationTokenSource();
            
            CurrentFighterType = fighterType;
            ReleaseCombatSlots();
            bool completedSuccessfully = await TryChangeCombatData(NpcElement.FightingStyle.RetrieveCombatData(fighterType));
            if (!completedSuccessfully) {
                return;
            }
            
            if (!NpcElement.IsUnconscious && CurrentBehaviour.Get() is not EquipWeaponBehaviour and not UnEquipWeaponBehaviour and not RagdollBehaviour and not StumbleBehaviour) {
                StopCurrentBehaviour(NpcAI?.InCombat ?? false);
            }
        }

        protected override UniTaskVoid ChangeCombatData(bool force = false) {
            if (!_weaponsEquipped || !ShouldWeaponsBeEquipped) {
                return default;
            }

            var inventory = NpcElement.Inventory;
            Item mainHandItem = inventory.ItemInSlots[EquipmentSlotType.MainHand];
            Item offHandItem = inventory.ItemInSlots[EquipmentSlotType.OffHand];
            FighterType fighterType = FighterType.GetFighterType(NpcElement, mainHandItem, offHandItem, ParentModel);
            if (fighterType == null) {
                return default;
            }
            
            if (force || CurrentFighterType == null || CurrentFighterType != fighterType) {
                UpdateFighterType(fighterType).Forget();
            }
            
            return default;
        }

        /// <summary>
        /// Setting Item Projectile and Projectile Data to null will trigger getting new prefab when shooting next arrow.
        /// </summary>
        void OnQuiverChanged(ICharacterInventory _) {
            _itemProjectile = null;
            _customProjectileData = default;
        }

        void UpdateDistancesToTargets() {
            DesiredDistanceToTarget = CurrentFighterType == FighterType.Fists
                ? NpcElement.FightingStyle.desiredDistanceToTargetWhenFistFighting
                : NpcElement.DefaultDesiredDistanceToTarget;
            MinDistanceToTarget = CurrentFighterType == FighterType.Fists
                ? NpcElement.FightingStyle.minDistanceToTargetWhenFistFighting
                : NpcElement.Controller.OriginalMinDistanceToTarget;
        }
    }
}
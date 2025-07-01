using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class CustomCombatBaseClass : EnemyBaseClass, IRefreshedByAttachment<CustomCombatAttachment>, ICloneAbleModel {
        public override ushort TypeForSerialization => SavedModels.CustomCombatBaseClass;

        // === Serialized Fields
        [SerializeField, BoxGroup("Spawn Settings")] bool startInSpawn;
        [SerializeField, BoxGroup("Spawn Settings"), ShowIf(nameof(startInSpawn))] bool canMoveInSpawn;
        [SerializeField, BoxGroup("Movement Settings")] bool usesCombatMovementAnimations;
        [SerializeField, BoxGroup("Movement Settings")] bool usesAlertMovementAnimations;
        [SerializeField, BoxGroup("Combat Enter Settings")] bool useRandomDelayOnCombatEnter = true;
        [SerializeField, BoxGroup("Combat Enter Settings"), ShowIf(nameof(useRandomDelayOnCombatEnter))] FloatRange randomDelayOnCombatEnter = new(0, 0.6f);
        [SerializeField, BoxGroup("Movement Settings")] bool canBePushed = true;

        // === Properties & Fields
        public override bool UsesCombatMovementAnimations => usesCombatMovementAnimations;
        public override bool UsesAlertMovementAnimations => usesAlertMovementAnimations;
        protected override bool CanBePushed => canBePushed;
        protected bool WeaponsEquipped { get; private set; }
        
        bool _canUseRandomDelay;
        bool _isInDelay;
        bool _fightingStyleBehavioursLoaded;

        // === Copying
        public CustomCombatBaseClass Copy() => (CustomCombatBaseClass) this.Clone();
        public virtual void CopyPropertiesTo(Model behaviourBase) {
            CustomCombatBaseClass customCombatBaseClass = (CustomCombatBaseClass)behaviourBase;
            customCombatBaseClass.CombatBehaviours = new List<EnemyBehaviourBase>();
            customCombatBaseClass.CombatBehavioursReferences = new List<ARAssetReference>();
        }
        
        // === Initialization
        public virtual void InitFromAttachment(CustomCombatAttachment spec, bool isRestored) {
            WeaponsAlwaysEquippedBase = spec.weaponsAlwaysEquipped;
            
            CustomCombatBaseClass baseClass = spec.CustomCombatBaseClass;
            startInSpawn = baseClass.startInSpawn;
            canMoveInSpawn = baseClass.canMoveInSpawn;
            usesCombatMovementAnimations = baseClass.usesCombatMovementAnimations;
            useRandomDelayOnCombatEnter = baseClass.useRandomDelayOnCombatEnter;
            randomDelayOnCombatEnter = baseClass.randomDelayOnCombatEnter;
            canBePushed = baseClass.canBePushed;
            canBeSlidInto = baseClass.canBeSlidInto;
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(() => {
                NpcElement.StartInSpawn = startInSpawn;
                NpcElement.CanMoveInSpawn = canMoveInSpawn;
            });
            base.OnInitialize();
        }

        protected override void AfterVisualLoaded(Transform parentTransform) {
            ParentModel.ListenTo(NpcElement.Events.AfterNpcVisibilityChanged, OnDistanceBandChanged, this);
            OnDistanceBandChanged(LocationCullingGroup.InNpcVisibilityBand(ParentModel.GetCurrentBandSafe(100)));
        }

        public override void OnWyrdConversionStarted() {
            EquipWeapons(true, out var equippedWeapons);
            if (NpcElement.TryGetElement(out NpcWeaponsHandler weaponsHandler)) {
                foreach (Item weapon in equippedWeapons) {
                    weaponsHandler.AttachWeaponToHand(weapon.Element<ItemEquip>());
                }
            }
        }

        void OnDistanceBandChanged(bool visible) {
            if (visible) {
                if (!_fightingStyleBehavioursLoaded) {
                    ChangeCombatData().Forget();
                }
            } else {
                TryChangeCombatData(null).Forget();
                _fightingStyleBehavioursLoaded = false;
            }
        }
        
        // === LifeCycle
        protected override async UniTaskVoid ChangeCombatData(bool force = false) {
            _fightingStyleBehavioursLoaded = true;
            
            bool success = await TryChangeCombatData(NpcElement.FightingStyle.RetrieveCombatData(this));
            if (!success) {
                _fightingStyleBehavioursLoaded = false;
                return;
            }
            
            StopCurrentBehaviour(NpcElement.IsInCombat());
        }

        protected override void OnEnterCombat() {
            ToggleWeapons();
            NpcAnimancer.UpdateCurrentCombatData(CurrentCombatData, StatsItem).Forget();
        }
        
        protected override void OnExitCombat() {
            base.OnExitCombat();
            _canUseRandomDelay = true;
            NpcAnimancer.UpdateCurrentCombatData(CurrentCombatData, null).Forget();
        }

        protected override void Tick(float deltaTime, NpcElement npc) {
            if (npc.GetCurrentTarget() == null) {
                StopCurrentBehaviour(false);
                return;
            }

            if (CurrentBehaviour.Get() == null && !_isInDelay) {
                if (_canUseRandomDelay && useRandomDelayOnCombatEnter) {
                    SelectNewBehaviourWithDelay().Forget();
                } else {
                    SelectNewBehaviour();
                }
            }
        }

        protected override void NotInCombatUpdate(float deltaTime) {
            ToggleWeapons();
        }

        // === Behaviours
        async UniTaskVoid SelectNewBehaviourWithDelay() {
            _isInDelay = true;
            _canUseRandomDelay = false;
            if (await AsyncUtil.DelayTime(this, randomDelayOnCombatEnter.RandomPick())) {
                SelectNewBehaviour();
            }

            _isInDelay = false;
        }
        
        // === Weapons
        protected void ToggleWeapons() {
            if (WeaponsAlwaysEquipped) {
                return;
            }
            
            bool alertedOrInCombat = ShouldWeaponsBeEquipped;
            if (WeaponsEquipped && !alertedOrInCombat) {
                UnEquipWeapons();
                return;
            }
            
            if (!WeaponsEquipped && alertedOrInCombat && !NpcElement.IsUnconscious) {
                EquipWeapons(true, out var equippedWeapons);
                
                if (!WeaponsAlwaysEquipped && TryGetElement(out EquipWeaponBehaviour equipWeaponBehaviour)) {
                    equipWeaponBehaviour.SetSettings(false, equippedWeapons.All(w => !w.IsRanged));
                    // --- If we failed to enter equip weapon behaviour, attach weapons manually
                    if (!TryStartBehaviour<EquipWeaponBehaviour>() && NpcElement.TryGetElement(out NpcWeaponsHandler weaponsHandler)) {
                        ItemEquip mainHandItemEquip = MainHandItem?.TryGetElement<ItemEquip>();
                        ItemEquip offHandItemEquip = OffHandItem?.TryGetElement<ItemEquip>();
                        if (mainHandItemEquip != null) {
                            weaponsHandler.AttachWeaponToHand(mainHandItemEquip);
                        }

                        if (offHandItemEquip != null) {
                            weaponsHandler.AttachWeaponToHand(offHandItemEquip);
                        }
                    }
                }
            }
        }

        protected override bool EquipWeapons(bool withResults, out List<Item> equippedItems, bool forceMelee = false, bool forceRanged = false) {
            WeaponsEquipped = true;
            return base.EquipWeapons(withResults, out equippedItems, forceMelee, forceRanged);
        }
        
        protected override void UnEquipWeapons() {
            base.UnEquipWeapons();
            WeaponsEquipped = false;
        }
    }
}
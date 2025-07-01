using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemEquip : Element<Item>, IItemAction, IRefreshedByAttachment<ItemEquipSpec> {
        public override ushort TypeForSerialization => SavedModels.ItemEquip;

        const uint DisabledShadowsRenderingLayerMask = 1;

        // === Fields
        public EquipmentType EquipmentType { get; private set; }
        public FinisherType FinisherType { get; private set; }
        public HitsToHitStop HitsToHitStop { get; private set; }
        public ItemRepresentationByNpc[] MobItems { get; private set; }
        public int AvailableGemSlots { get; private set; }
        public int MaxGemSlots { get; private set; }
        
        // === Properties
        public ItemActionType Type => IsEquipped ? ItemActionType.Unequip : ItemActionType.Equip;
        public bool IsEquipped => Item.IsEquipped;
        public Item Item => ParentModel;
        bool CanUseAdditionalHands => Item.Character?.CanUseAdditionalHands ?? false;
        
        CharacterHandBase _weaponInstance;
        ARAssetReference _weaponSpawnedPrefab;
        ARAssetReference _armorSpawnedPrefab;
        bool _isEquipping;

        // === Events
        public new class Events {
            public static readonly Event<ICharacterInventory, GameObject> WeaponEquipped = new (nameof(WeaponEquipped));
        }
        
        // === Constructors
        
        public void InitFromAttachment(ItemEquipSpec spec, bool isRestored) {
            EquipmentType = spec.EquipmentType;
            MobItems = spec.RetrieveMobItemsInstance();
            FinisherType = spec.FinisherType;
            HitsToHitStop = spec.HitsToHitStop;
            AvailableGemSlots = spec.GemSlots > spec.MaxGemSlots ? spec.MaxGemSlots : spec.GemSlots;
            MaxGemSlots = spec.MaxGemSlots;
        }

        public static ItemEquip JsonCreate() => new ItemEquip();

        // === Initialization

        protected override void OnInitialize() {
            if (AvailableGemSlots >= 1) {
                ParentModel.AddElement(new ItemGems(AvailableGemSlots, MaxGemSlots));
            }
            InitListeners();
        }

        protected override void OnRestore() {
            InitListeners();
            ParentModel.AfterFullyInitialized(AfterParentFullyInitialized);
        }

        void AfterParentFullyInitialized() {
            switch (ParentModel.Owner?.EquipTarget) {
                case NpcElement { CanEquip: true } npc:
                    npc.OnCompletelyInitialized(_ => OnEquip());
                    break;
                case NpcDummy { CanEquip: true } npcDummy:
                    npcDummy.OnCompletelyInitialized(_ => OnEquip());
                    break;
                case Hero:
                    OnEquip();
                    break;
            }
        }

        void InitListeners() {
            ParentModel.ListenTo(Item.Events.Equipped, OnEquip, this);
            ParentModel.ListenTo(Item.Events.Unequipped, OnUnequip, this);
        }
        
        // === Operations

        public void Submit() {
            if (ItemActionType.IsEquipAction(Type)) {
                Item.CharacterInventory?.Equip(ParentModel);
            } else {
                Item.CharacterInventory?.Unequip(ParentModel);
            }
        }
        public void AfterPerformed() {}
        public void Perform() {}
        public void Cancel() {}

        public EquipmentSlotType GetBestEquipmentSlotType() {
            if (EquipmentType == EquipmentType.Magic) {
                return SelectBestSlotFrom(CanUseAdditionalHands ? EquipmentSlotType.AllHands : EquipmentSlotType.Hands);
            } else if (EquipmentType == EquipmentType.Ring) {
                return SelectBestSlotFrom(EquipmentSlotType.Rings);
            } else if (EquipmentType == EquipmentType.QuickUse || EquipmentType == EquipmentType.Throwable) {
                return SelectBestSlotFrom(EquipmentSlotType.ManualQuickSlots);
            } else if (EquipmentType == EquipmentType.OneHanded) {
                return SelectBestSlotFrom(CanUseAdditionalHands ? EquipmentSlotType.AllHands : EquipmentSlotType.Hands);
            } else if (EquipmentType == EquipmentType.Shield && CanUseAdditionalHands) {
                return SelectBestSlotFrom(EquipmentSlotType.AllHands);
            } else {
                return EquipmentType.MainSlotType;
            }
        }

        public EquipmentSlotType GetMainSlot() {
            if (EquipmentType.CustomMainSlotTypes.Contains(EquipmentType)) {
                return Item.CharacterInventory?.SlotWith(Item) ?? EquipmentType.MainSlotType;
            }
            return EquipmentType.MainSlotType;
        }

        public Transform GetItemSocket(IWithItemSockets sockets) {
            var type = GetMainSlot();
            if (type == EquipmentSlotType.MainHand) {
                return EquipmentType == EquipmentType.Bow ? sockets.OffHandSocket : sockets.MainHandSocket;
            } else if (type == EquipmentSlotType.OffHand) {
                return sockets.OffHandSocket;
            } else if (type == EquipmentSlotType.AdditionalMainHand) {
                return sockets.AdditionalMainHandSocket;
            } else if (type == EquipmentSlotType.AdditionalOffHand) {
                return sockets.AdditionalOffHandSocket;
            } else {
                return sockets.RootSocket;
            }
        }
        
        public void PlayEquipToggleSound(IAlive owner, bool equip) {
            ARAudioType<Item> audioType;
            if (EquipmentType.ProvidesCloth) {
                audioType = equip ? ArmorAudioType.EquipArmor : ArmorAudioType.UnEquipArmor;
            } else if (EquipmentType == EquipmentType.Magic || EquipmentType == EquipmentType.MagicTwoHanded) {
                audioType = equip ? ItemAudioType.EquipMagic : ItemAudioType.UnEquipMagic;
            } else if (EquipmentType == EquipmentType.Bow) {
                audioType = equip ? ItemAudioType.EquipBow : ItemAudioType.UnEquipBow;
            } else {
                audioType = equip ? ItemAudioType.MeleeEquip : ItemAudioType.MeleeUnEquip;
            }
            ParentModel.PlayAudioClip(audioType.RetrieveFrom(Item), true);
        }
        
        public ARAssetReference GetHeroItem(Hero hero) {
            using var heroAbstracts = hero.Template.AbstractTypes;
            Gender heroGender = hero.GetGender();
            foreach (var mobItem in MobItems) {
                if (CanEquip(heroAbstracts, heroGender, ParentModel.EquippedInSlotOfType, mobItem)) {
                    return mobItem.itemPrefab;
                }
            }
            
            return null;
        }

        EquipmentSlotType SelectBestSlotFrom(EquipmentSlotType[] supposedTypes) {
            var inventory = Item.CharacterInventory;
            if (inventory == null) {
                return supposedTypes[0];
            }
            
            var slotType = inventory.SlotWith(ParentModel);
            if (slotType != null) {
                return slotType;
            }

            foreach (var type in supposedTypes) {
                if (!inventory.IsEquipped(type)) {
                    return type;
                }
            }

            return supposedTypes[0];
        }

        // === Equipping
        
        void OnEquip() {
            if (HasBeenDiscarded) return;
            
            if (!IsEquipped || ParentModel.Owner?.EquipTarget == null) return;

            if (_isEquipping) {
                return;
            }

            if (ParentModel.Owner.EquipTarget is Hero hero) {
                _isEquipping = true;
                hero.OnVisualLoaded(() => {
                    if (!_isEquipping) {
                        return;
                    }
                    HeroEquip(hero);
                    PlayEquipToggleSound(hero, true);
                    _isEquipping = false;
                });
            } else if (ParentModel.Owner.EquipTarget is INpcEquipTarget { CanEquip: true } npcTarget) {
                NpcEquip(npcTarget);
            }
        }

        void HeroEquip(Hero hero) {
            ARAssetReference itemPrefab = GetHeroItem(hero);

            if (itemPrefab?.IsSet ?? false) {
                if (EquipmentType.ProvidesCloth) {
                    if (ParentModel.EquipmentType == EquipmentType.Helmet &&
                        World.Only<DisableHeroHelmetSetting>().Enabled) {
                        return;
                    }

                    if (Hero.TppActive) {
                        hero.BodyClothes.Equip(itemPrefab);
                    } else if (EquipmentType == EquipmentType.Gauntlets || EquipmentType == EquipmentType.Cuirass) {
                        hero.HandClothes.Equip(itemPrefab, BaseClothes.ShadowsOverride.ForceOff);
                    }
                    foreach (var clothes in World.All<CustomHeroClothes>()) {
                        clothes.Equip(itemPrefab);
                    }
                    _armorSpawnedPrefab = itemPrefab;
                } else if (EquipmentType.IsWeapon) {
                    ReleaseWeaponPrefab();
                    _weaponSpawnedPrefab = itemPrefab;
                    ARAsyncOperationHandle<GameObject> handle = _weaponSpawnedPrefab.LoadAsset<GameObject>();
                    Transform itemParent = GetItemSocket(hero);
                    var heroWeaponShadowCastingMode = Hero.TppActive ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    handle.OnCompleteForceAsync(h => OnWeaponLoaded(h, itemParent, heroWeaponShadowCastingMode));

                    foreach (var clothes in World.All<CustomHeroClothes>()) {
                        clothes.SpawnWeapon(itemPrefab, this);
                    }
                }
            }
        }

        void NpcEquip(INpcEquipTarget npcTarget) {
            var npcAbstracts = npcTarget.Template.AbstractTypes;
            Gender npcGender = npcTarget.GetGender();
            ItemRepresentationByNpc itemToSpawn = MobItems.FirstOrDefault(m =>
                CanEquip(npcAbstracts, npcGender, ParentModel.EquippedInSlotOfType, m));
            npcAbstracts.Release();
            if (!(itemToSpawn.itemPrefab?.IsSet ?? false)) {
                Log.Minor?.Info($"Item: {LogUtils.GetDebugName(ParentModel)} has no visual prefab attached to equip for {LogUtils.GetDebugName(npcTarget)} for template: {ParentModel.Template.name}", ParentModel.Template);
                return;
            }
            if (npcTarget.Clothes == null) {
                Log.Important?.Error($"Trying to equip item for {LogUtils.GetDebugName(GenericParentModel)} but clothes are null");
                return;
            }
            
            NpcEquip(npcTarget, itemToSpawn, GetItemSocket(npcTarget));
        }

        void NpcEquip(INpcEquipTarget npcTarget, ItemRepresentationByNpc itemToSpawn, Transform itemParent) {
            if (EquipmentType.IsAccessory) {
                return;
            }
            if (EquipmentType.ProvidesCloth) {
                npcTarget.Clothes.Equip(itemToSpawn.itemPrefab);
                _armorSpawnedPrefab = itemToSpawn.itemPrefab;
            } else {
                if (npcTarget is NpcElement { CanDetachWeaponsToBelts: true }) {
                    return;
                }
                
                if (itemToSpawn.itemPrefab is not { IsSet: true }) {
                    Log.Important?.Error($"Trying to equip empty weapon for {GenericParentModel}");
                    return;
                }

                if (_weaponInstance != null) {
                    Log.Important?.Warning($"Trying to equip already equipped weapon for {GenericParentModel} - {itemToSpawn.itemPrefab.RuntimeKey}");
                    return;
                }

                ReleaseWeaponPrefab();
                _weaponSpawnedPrefab = itemToSpawn.itemPrefab;
                ARAsyncOperationHandle<GameObject> handle = _weaponSpawnedPrefab.LoadAsset<GameObject>();
                handle.OnComplete(h => OnWeaponLoaded(h, itemParent));
            }
        }

        void OnWeaponLoaded(ARAsyncOperationHandle<GameObject> handle, Transform parent, Optional<ShadowCastingMode> shadowCastingMode = default) {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null || handle.IsCancelled) {
                ReleaseWeaponPrefab();
                return;
            }
            
            var equipTarget = ParentModel.Owner?.EquipTarget;
            if (!ParentModel.IsEquipped || equipTarget is not ICharacter character) {
                ReleaseWeaponPrefab();
                return;
            }
            
            GameObject itemInstance = Object.Instantiate(handle.Result, parent);
            _weaponInstance = itemInstance.GetComponent<CharacterHandBase>();
            if (_weaponInstance == null) {
                Log.Critical?.Error($"Trying to equip item without CharacterHandBase component for {LogUtils.GetDebugName(ParentModel)} on {LogUtils.GetDebugName(ParentModel.Owner)}");
                Object.Destroy(itemInstance);
                ReleaseWeaponPrefab();
                return;
            }
            
            itemInstance.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                linkedLifetime = true,
                movable = true
            });

            World.BindView(ParentModel, _weaponInstance, true, true);
            character.AttachWeapon(_weaponInstance);
            Item.CharacterInventory?.Trigger(Events.WeaponEquipped, itemInstance);
            // --- Disable shadow casting for Hero
            if (shadowCastingMode.HasValue) {
                UnityEngine.Pool.ListPool<DrakeMeshRenderer>.Get(out var drakeMeshRenderers);
                itemInstance.GetComponentsInChildren(drakeMeshRenderers);
                foreach (var drakeMeshRenderer in drakeMeshRenderers) {
                    ref var meshDesc = ref drakeMeshRenderer.SerializableRenderMeshDescription;
                    meshDesc.OverrideShadowsCasting(shadowCastingMode.Value);
                    if (shadowCastingMode.Value == ShadowCastingMode.Off) {
                        meshDesc.OverrideRenderingLayerMask(DisabledShadowsRenderingLayerMask);
                    }
                }
                UnityEngine.Pool.ListPool<DrakeMeshRenderer>.Release(drakeMeshRenderers);

                UnityEngine.Pool.ListPool<KandraRenderer>.Get(out var kandraRenderers);
                itemInstance.GetComponentsInChildren(kandraRenderers);
                foreach (var renderer in kandraRenderers) {
                    var filterSettings = renderer.rendererData.filteringSettings;
                    if ((filterSettings.shadowCastingMode != shadowCastingMode.Value) | 
                        ((shadowCastingMode.Value == ShadowCastingMode.Off) & (filterSettings.renderingLayersMask != DisabledShadowsRenderingLayerMask))) {
                        filterSettings.shadowCastingMode = shadowCastingMode.Value;
                        if (shadowCastingMode.Value == ShadowCastingMode.Off) {
                            filterSettings.renderingLayersMask = DisabledShadowsRenderingLayerMask;
                        }
                        renderer.SetFilteringSettings(filterSettings);
                    }
                }
                UnityEngine.Pool.ListPool<KandraRenderer>.Release(kandraRenderers);
            }
        }

        // === Unequipping
        void OnUnequip(EquipmentSlotType unEquippedSlotType) {
            if (IsEquipped || ParentModel.Owner?.EquipTarget == null) return;

            _isEquipping = false;
            bool armorSet = _armorSpawnedPrefab?.IsSet ?? false;
            bool weaponSet = _weaponSpawnedPrefab?.IsSet ?? false;
            IEquipTarget equipTarget = ParentModel.Owner.EquipTarget;
            if (equipTarget is Hero h) {
                HeroUnequip(h, armorSet, weaponSet, unEquippedSlotType);
                PlayEquipToggleSound(h, false);
            } else if (equipTarget is INpcEquipTarget { CanEquip: true } npcTarget) {
                NpcUnequip(npcTarget, armorSet, weaponSet); 
            }
            
            _armorSpawnedPrefab = null;
            if (Item.IsWeapon && equipTarget is NpcElement { CanDetachWeaponsToBelts: true } npc) {
                npc.WeaponsHandler.AttachWeaponToBelt(this);
                return;
            }
            ReleaseWeaponPrefab();
        }

        void HeroUnequip(Hero hero, bool armorSet, bool weaponSet, EquipmentSlotType unEquippedSlotType) {
            if (armorSet) {
                hero.HandClothes.Unequip(_armorSpawnedPrefab);
                hero.BodyClothes.Unequip(_armorSpawnedPrefab);
                foreach (var clothes in World.All<CustomHeroClothes>()) {
                    clothes.Unequip(GetHeroItem(hero));
                }
            }
            
            if (weaponSet) {
                foreach (var clothes in World.All<CustomHeroClothes>()) {
                    if (EquipmentType == EquipmentType.TwoHanded) { //ensure that both hands are empty
                        clothes.DespawnWeapon(EquipmentSlotType.MainHand);
                        clothes.DespawnWeapon(EquipmentSlotType.OffHand);
                    }
                    clothes.DespawnWeapon(unEquippedSlotType);
                }
            }
        }

        void NpcUnequip(INpcEquipTarget npcTarget, bool armorSet, bool weaponSet) {
            if (armorSet) {
                if (npcTarget.Clothes is BaseClothes baseClothes) {
                    baseClothes.SafeUnequip(_armorSpawnedPrefab);
                } else {
                    npcTarget.Clothes?.Unequip(_armorSpawnedPrefab);
                }
                
            }
        }

        void ReleaseWeaponPrefab() {
            if (_weaponInstance != null) {
                if (ParentModel.Owner?.EquipTarget is ICharacter character) {
                    character.DetachWeapon(_weaponInstance);
                }
                _weaponInstance.Discard();
                _weaponInstance = null;
            }
            _weaponSpawnedPrefab?.ReleaseAsset();
            _weaponSpawnedPrefab = null;
        }
        
        // === Helpers
        public void ChangeHelmetVisibility(bool isVisible) {
            if (ParentModel.EquipmentType != EquipmentType.Helmet) {
                return;
            }

            if (ParentModel.Owner is not Hero hero) {
                return;
            }

            if (!isVisible && _armorSpawnedPrefab != null) {
                HeroUnequip(hero, true, false, EquipmentSlotType.Helmet);
                _armorSpawnedPrefab = null;
            } else if (isVisible && _armorSpawnedPrefab == null) {
                HeroEquip(hero);
            }
        }
        
        public static bool CanEquip(List<NpcTemplate> abstracts, Gender gender, EquipmentSlotType equipmentSlotType, ItemRepresentationByNpc representation) {
            bool abstractNpcFulfilled = representation.AbstractNPCs.All(abstracts.Contains);
            bool genderFulfilled = representation.Gender == Gender.None || representation.Gender == gender;
            bool itemHandFulfilled = equipmentSlotType == null || CanEquipInHand(representation.Hand, equipmentSlotType);
            return abstractNpcFulfilled && genderFulfilled && itemHandFulfilled;
        }
        
        // === Debug
        
        public ARAssetReference GetDebugHeroItem() {
            return GetDebugHeroItem(MobItems);
        }
        
        public static ARAssetReference GetDebugHeroItem(ItemRepresentationByNpc[] mobItems) {
            using var heroAbstracts = DebugReferences.Get.HeroClass.AbstractTypes;
            Gender heroGender = Gender.Male;
            foreach (var mobItem in mobItems) {
                if (CanEquip(heroAbstracts.value, heroGender, null, mobItem)) {
                    return mobItem.itemPrefab;
                }
            }

            return null;
        }
        
        public static bool CanEquipInHand(ItemEquipHand itemEquipHand, EquipmentSlotType equipmentSlotType) {
            return itemEquipHand switch {
                ItemEquipHand.None => true,
                ItemEquipHand.MainHand => equipmentSlotType == EquipmentSlotType.MainHand ||
                                          equipmentSlotType == EquipmentSlotType.AdditionalMainHand,
                ItemEquipHand.OffHand => equipmentSlotType == EquipmentSlotType.OffHand ||
                                         equipmentSlotType == EquipmentSlotType.AdditionalOffHand,
                _ => true
            };
        }
    }
}

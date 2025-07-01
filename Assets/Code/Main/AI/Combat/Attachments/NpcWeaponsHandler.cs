using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.AI.Combat.Attachments {
    public partial class NpcWeaponsHandler : Element<NpcElement> {
        static readonly IWithUnityRepresentation.Options WeaponOptions = new IWithUnityRepresentation.Options {
            linkedLifetime = true,
            movable = true,
            requiresEntitiesAccess = true
        };

        static readonly List<NpcTemplate> NPCAbstracts = new();

        public sealed override bool IsNotSaved => true;

        readonly Dictionary<ItemEquip, WeaponHandle> _itemToWeaponHandle = new();
        readonly Dictionary<ItemEquip, ARAsyncOperationHandle<GameObject>> _itemToLoadHandle = new();
        readonly Dictionary<ItemEquip, Action> _onWeaponsLoaded = new();
        bool _isLoadingWeapons = true;

        public new static class Events {
            public static readonly Event<NpcElement, GameObject> NpcWeaponLoaded = new(nameof(NpcWeaponLoaded));
            public static readonly Event<NpcElement, NpcElement> WeaponsLoadedToBelts = new(nameof(WeaponsLoadedToBelts));
        }

        // === Initialization
        public void Init(bool wasItemsAddedToInventory) {
            _isLoadingWeapons = true;
            if (wasItemsAddedToInventory) {
                LoadWeapons().Forget();
            } else {
                ParentModel.ListenTo(NpcElement.Events.ItemsAddedToInventory, _ => LoadWeapons().Forget(), this);
            }
        }

        async UniTaskVoid LoadWeapons() {
            NPCAbstracts.Clear();
            var abstractTypes = ParentModel.Template.AbstractTypes;
            NPCAbstracts.AddRange(abstractTypes.value);
            abstractTypes.Release();
            Gender npcGender = ParentModel.GetGender();

            List<UniTask<GameObject>> tasks = new();
            foreach (var weapon in ParentModel.NpcItems.AllWeapons) {
                if (TryLoadWeaponInternal(weapon, NPCAbstracts, npcGender, out var handle, out var itemToSpawn, out var itemEquip)) {
                    handle.OnComplete(h => {
                        if (itemEquip.HasBeenDiscarded) {
                            Log.Important?.Warning($"{weapon.DebugName} has been discarded during loading");
                            return;
                        }
                        OnWeaponLoaded(h, GetBeltItemSocket(itemEquip), itemEquip, itemToSpawn.itemPrefab);
                    });
                    tasks.Add(handle.ToUniTask());
                }
            }

            NPCAbstracts.Clear();
            await tasks;
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            _isLoadingWeapons = false;
            _onWeaponsLoaded.Values.ForEach(a => a?.Invoke());
            _onWeaponsLoaded.Clear();

            ParentModel.Trigger(Events.WeaponsLoadedToBelts, ParentModel);
        }

        public void LoadNewWeapon(Item weapon, bool equipAfterLoad, WeaponEquipHand overrideHand = WeaponEquipHand.None) {
            using var abstractTypes = ParentModel.Template.AbstractTypes;
            if (!TryLoadWeaponInternal(weapon, abstractTypes, ParentModel.GetGender(), out var handle, out var itemToSpawn, out var itemEquip)) {
                return;
            }

            _itemToLoadHandle.Add(itemEquip, handle);

            if (equipAfterLoad) {
                handle.OnComplete(h => {
                    OnWeaponLoaded(h, GetBeltItemSocket(itemEquip), itemEquip, itemToSpawn.itemPrefab);
                    AttachWeaponToHand(itemEquip, overrideHand);
                });
            } else {
                handle.OnComplete(h =>
                    OnWeaponLoaded(h, GetBeltItemSocket(itemEquip), itemEquip, itemToSpawn.itemPrefab));
            }
        }

        bool TryLoadWeaponInternal(Item weapon, List<NpcTemplate> npcAbstracts, Gender npcGender, out ARAsyncOperationHandle<GameObject> handle, out ItemRepresentationByNpc itemToSpawn, out ItemEquip itemEquip) {
            if (!weapon.TryGetElement(out itemEquip)) {
                itemToSpawn = default;
                handle = default;
                return false;
            }

            itemToSpawn = itemEquip.MobItems.FirstOrDefault(m => ItemEquip.CanEquip(npcAbstracts, npcGender, weapon.EquippedInSlotOfType, m));
            if (itemToSpawn.itemPrefab is not { IsSet: true }) {
                Log.Important?.Error($"Trying to equip empty weapon for {GenericParentModel}");
                handle = default;
                return false;
            }

            handle = itemToSpawn.itemPrefab.LoadAsset<GameObject>();
            return true;
        }

        public void UnloadWeapon(Item weapon) {
            if (!weapon.TryGetElement(out ItemEquip itemEquip)) {
                return;
            }

            if (_itemToWeaponHandle.TryGetValue(itemEquip, out WeaponHandle handle)) {
                handle.handle.ReleaseAsset();
                handle.instance.Discard();
                _itemToWeaponHandle.Remove(itemEquip);
            } else if (_itemToLoadHandle.TryGetValue(itemEquip, out ARAsyncOperationHandle<GameObject> loadHandle)) {
                loadHandle.Release();
                _itemToLoadHandle.Remove(itemEquip);
            }
        }

        void OnWeaponLoaded(ARAsyncOperationHandle<GameObject> handle, Transform parent, ItemEquip itemEquip, ARAssetReference weaponHandle) {
            _itemToLoadHandle.Remove(itemEquip);
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null || handle.IsCancelled || HasBeenDiscarded || ParentModel is not { HasBeenDiscarded: false }) {
                weaponHandle.ReleaseAsset();
                return;
            }

            GameObject itemInstance = Object.Instantiate(handle.Result, parent);
            itemInstance.SetUnityRepresentation(WeaponOptions);
            var weaponInstance = itemInstance.GetComponent<CharacterHandBase>();
            World.BindView(itemEquip.Item, weaponInstance, true, true);
            _itemToWeaponHandle.Add(itemEquip, new WeaponHandle(weaponHandle, weaponInstance));
            ParentModel.Trigger(Events.NpcWeaponLoaded, itemInstance);
        }

        // === Public API
        public void AttachWeaponToHand(ItemEquip itemEquip, WeaponEquipHand hand = WeaponEquipHand.None) {
            if (_isLoadingWeapons) {
                _onWeaponsLoaded[itemEquip] = () => AttachWeaponToHand(itemEquip, hand);
                return;
            }

            if (itemEquip.Item.IsMagic) {
                // We don't equip magic items to hands.
                return;
            }

            if (!_itemToWeaponHandle.TryGetValue(itemEquip, out WeaponHandle weapon)) {
                Log.Important?.Error($"Failed to find weapon assigned to: {itemEquip.Item}, {this}");
                return;
            }

            Transform socket;
            if (hand == WeaponEquipHand.None) {
                socket = itemEquip.GetItemSocket(ParentModel);
            } else {
                socket = hand == WeaponEquipHand.MainHand ? ParentModel.MainHand : ParentModel.OffHand;
            }

            Transform weaponTransom = weapon.instance.transform;
            weaponTransom.SetParent(socket, false);
            weaponTransom.SetLocalPositionAndRotation(weapon.originalPosition, weapon.originalRotation);
            ParentModel.AttachWeapon(weapon.instance);
            itemEquip.Item.CharacterInventory?.Trigger(ItemEquip.Events.WeaponEquipped, weapon.instance.gameObject);
        }

        public void AttachWeaponToBelt(ItemEquip itemEquip) {
            if (_isLoadingWeapons) {
                _onWeaponsLoaded[itemEquip] = () => AttachWeaponToBelt(itemEquip);
                return;
            }

            if (itemEquip.Item.IsMagic) {
                // We don't equip magic items to hands.
                return;
            }

            if (!_itemToWeaponHandle.TryGetValue(itemEquip, out WeaponHandle weapon)) {
                Log.Important?.Error($"Failed to find weapon assigned to: {itemEquip.Item}, {this}");
                return;
            }

            Transform weaponTransom = weapon.instance.transform;
            // --- Check if weapon is not already attached to belt
            if (ParentModel.BeltItemSockets.Contains(weaponTransom.parent)) {
                return;
            }

            weaponTransom.SetParent(GetBeltItemSocket(itemEquip), false);
            weaponTransom.SetLocalPositionAndRotation(weapon.originalPosition, weapon.originalRotation);
            ParentModel.DetachWeapon(weapon.instance);
        }

        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            foreach (var weaponHandle in _itemToWeaponHandle.Values) {
                if (!weaponHandle.instance.WasDiscarded) {
                    weaponHandle.instance.Discard();
                }
                weaponHandle.handle.ReleaseAsset();
            }

            _itemToWeaponHandle.Clear();
        }

        // === Helpers
        public bool IsMainHandUsingBackEqSlots() {
            var item = ParentModel.Inventory.EquippedItem(EquipmentSlotType.MainHand);
            if (item == null) {
                return false;
            }

            if (item.IsTwoHanded || item.IsRanged || item.IsShield) {
                return true;
            }

            return false;
        }

        Transform GetBeltItemSocket(ItemEquip itemEquip) {
            if (HasBeenDiscarded || ParentModel is not { HasBeenDiscarded: false }) {
                return null;
            }
            
            if (itemEquip.Item.IsRanged || itemEquip.Item.IsShield) {
                Transform slot = ParentModel.BackEqSlot;
                UpdateSlotPositionAndRotation(itemEquip.Item, slot);
                return slot;
            }

            if (itemEquip.Item.IsTwoHanded) {
                Transform slot = ParentModel.BackWeaponEqSlot;
                UpdateSlotPositionAndRotation(itemEquip.Item, slot);
                return slot;
            }

            int weaponsInMainHand = ParentModel.MainHandEqSlot.GetComponentsInChildren<CharacterHandBase>().Length;
            return weaponsInMainHand > 0 ? ParentModel.OffHandEqSlot : ParentModel.MainHandEqSlot;
        }

        static void UpdateSlotPositionAndRotation(Item item, Transform slotTransform) {
            Vector3 offset = Vector3.zero;
            Vector3 rotation = Vector3.zero;
            foreach (var slotOffset in GameConstants.Get.BackWeaponSlotOffset) {
                if (slotOffset.TryGetBackSlotOffset(item, out offset, out rotation)) {
                    break;
                }
            }
            slotTransform.localPosition = offset;
            slotTransform.localEulerAngles = rotation;
        }
        
        readonly struct WeaponHandle {
            public readonly ARAssetReference handle;
            public readonly CharacterHandBase instance;
            public readonly Vector3 originalPosition;
            public readonly Quaternion originalRotation;

            public WeaponHandle(ARAssetReference handle, CharacterHandBase instance) {
                this.handle = handle;
                this.instance = instance;
                var transform = instance.transform;
                originalPosition = transform.localPosition;
                originalRotation = transform.localRotation;
            }
        }
    }
}
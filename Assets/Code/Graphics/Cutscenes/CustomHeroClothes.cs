using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    /// <summary>
    /// For cutscenes and inventory preview
    /// </summary>
    public partial class CustomHeroClothes : BaseClothes<ICustomClothesOwner>, IWithItemSockets {
        public override ushort TypeForSerialization => SavedModels.CustomHeroClothes;

        static readonly IWithUnityRepresentation.Options WeaponOptions = new IWithUnityRepresentation.Options {
            linkedLifetime = true,
            movable = true,
            requiresEntitiesAccess = true
        };

        [Saved] bool _spawnHelmet;
        readonly Dictionary<EquipmentSlotType, ReferenceInstance<GameObject>> _handleBySlot = new();

        public Transform HeadSocket => ParentModel.HeadSocket;
        public Transform MainHandSocket => ParentModel.MainHandSocket;
        public Transform MainHandWristSocket => ParentModel.MainHandWristSocket;
        public Transform OffHandSocket => ParentModel.OffHandSocket;
        public Transform OffHandWristSocket => ParentModel.OffHandWristSocket;
        public Transform HipsSocket => ParentModel.HipsSocket;
        public Transform RootSocket => ParentModel.RootSocket;
        
        protected override Transform ParentTransform => RootSocket;
        protected override uint? LightRenderLayerMask => ParentModel.LightRenderLayerMask;
        int? WeaponLayer => ParentModel.WeaponLayer;
        bool SpawnHelmet => _spawnHelmet && !World.Only<DisableHeroHelmetSetting>().Enabled;

        public new static class Events {
            public static readonly Event<CustomHeroClothes, GameObject> WeaponEquipped = new(nameof(WeaponEquipped));
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        CustomHeroClothes() { }
        public CustomHeroClothes(bool spawnHelmet = true) {
            _spawnHelmet = spawnHelmet;
        }

        public UniTask LoadEquipped() {
            var tasks = new List<UniTask>();
            var hero = Hero.Current;
            foreach (var item in hero.HeroItems.DistinctEquippedItems()) {
                var equip = item.Element<ItemEquip>();

                if (!SpawnHelmet && equip.EquipmentType == EquipmentType.Helmet) {
                    continue;
                }
                
                if (equip.GetHeroItem(hero) is { IsSet: true } prefab) {
                    if (equip.EquipmentType.ProvidesCloth) {
                        tasks.Add(EquipTask(prefab));
                    } else if (item.IsWeapon) {
                        SpawnWeapon(prefab, equip);
                    }
                }
            }

            return UniTask.WhenAll(tasks);
        }

        public void UnloadAll() {
            foreach (var cloth in _equipped.Keys.ToArray()) {
                Unequip(cloth);
            }
        }

        public void SpawnWeapon(ARAssetReference weapon, ItemEquip equip) {
            SpawnWeapon(weapon, equip, equip.GetItemSocket(this));
        }

        public void SpawnWeapon(ARAssetReference weapon, ItemEquip equip, Transform host) {
            var slot = equip.GetMainSlot();
            DespawnWeapon(slot);

            var spawnedReference = new ReferenceInstance<GameObject>(weapon.DeepCopy());
            spawnedReference.Instantiate(host, go => OnWeaponLoaded(go, equip));

            _handleBySlot.Add(slot, spawnedReference);
        }

        void OnWeaponLoaded(GameObject instance, ItemEquip equip) {
            if (instance == null) {
                DespawnWeapon(equip.GetMainSlot());
                return;
            }
            
            SetupWeaponVisual(instance);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (instance.TryGetComponent(out CharacterHandBase handBase)) {
                handBase.AttachToCustomHeroClothes(this, equip);
            }
            
            this.Trigger(Events.WeaponEquipped, instance);
        }

        void SetupWeaponVisual(GameObject instance) {
            // Drake
            var drakeLodGroup = instance.GetComponentInChildren<DrakeLodGroup>();
            if (drakeLodGroup && !drakeLodGroup.gameObject.HasComponent<ForceDrakeLoad>()) {
                drakeLodGroup.gameObject.AddComponent<ForceDrakeLoad>();
            }
            instance.SetUnityRepresentation(WeaponOptions);
            instance.SetEcsRenderingLayer(WeaponLayer, LightRenderLayerMask);
            if (LightRenderLayerMask.HasValue | WeaponLayer.HasValue) {
                // Kandra
                foreach (var renderer in instance.GetComponentsInChildren<KandraRenderer>(true)) {
                    var newFilterSettings = renderer.rendererData.filteringSettings;
                    if (LightRenderLayerMask != null) {
                        newFilterSettings.renderingLayersMask = LightRenderLayerMask.Value;
                    }

                    if (WeaponLayer != null) {
                        renderer.gameObject.layer = WeaponLayer.Value;
                    }
                    renderer.SetFilteringSettings(newFilterSettings);
                }
                // Standard
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true)) {
                    if (LightRenderLayerMask != null) {
                        renderer.renderingLayerMask = LightRenderLayerMask.Value;
                    }

                    if (WeaponLayer != null) {
                        renderer.gameObject.layer = WeaponLayer.Value;
                    }
                }
            }

            var visualData = instance.GetComponentInChildren<ProjectileVisualData>();
            if (visualData != null && visualData.trailHolder != null) {
                visualData.trailHolder.SetActive(false);
            }
        }

        public void DespawnWeapon(EquipmentSlotType slot) {
            if (_handleBySlot.Remove(slot, out var handle)) {
                if (handle.Instance != null && handle.Instance.TryGetComponent(out CharacterHandBase handBase)) {
                    handBase.DetachFromCustomHeroClothes(this);
                }
                handle.ReleaseInstance();
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            UnloadAll();
            foreach (var handle in _handleBySlot.Values) {
                handle.ReleaseInstance();
            }
        }
    }
}
using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using FMODUnity;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemSlotUI : MonoBehaviour, INewThingCarrier {
        [Title("Components")] 
        [SerializeField] ItemIconComponent icon;
        [SerializeField] ItemSelectionComponent selection;

        [SerializeField, CanBeNull] ItemBackgroundComponent mainBackground;
        [SerializeField, CanBeNull] ItemQuantityComponent quantity;
        [SerializeField, CanBeNull] ItemQualityComponent quality;
        [SerializeField, CanBeNull] ItemEquippedComponent equipped;
        [SerializeField, CanBeNull] ItemInLoadoutComponent inLoadout;
        [SerializeField, CanBeNull] ItemWeightComponent weight;
        [SerializeField, CanBeNull] EmptySlotComponent empty;
        [SerializeField, CanBeNull] ItemTheftComponent theft;
        [SerializeField, CanBeNull] PlainFoodLevelComponent plainFoodLevel;
        
        [Title("Audio")] 
        [SerializeField] bool useCustomSound;
        [SerializeField, ShowIf(nameof(useCustomSound))] public EventReference hoverSound;

        public IModelNewThing NewThingModel { get; private set; }
        public event Action onNewThingRefresh;

        void Start() {
            SetupSounds();
        }

        public void Setup(Item item, View view, ItemDescriptorType itemDescriptorType = null) {
            var data = (item, view, itemDescriptorType);
            icon?.TryRefresh(data);
            mainBackground?.TryRefresh(data);
            quantity?.TryRefresh(data);
            quality?.TryRefresh(data);
            equipped?.TryRefresh(data);
            inLoadout?.TryRefresh(data);
            weight?.TryRefresh(data);
            empty?.TryRefresh(data);
            selection?.TryRefresh(data);
            theft?.TryRefresh(data);
            plainFoodLevel?.TryRefresh(data);

            if (World.Any<CharacterSheetUI>()) {
                NewThingModel = item;
                onNewThingRefresh?.Invoke();
            }
        }

        public void SetupOnlyIcon(Item item, View view, ItemDescriptorType itemDescriptorType = null) {
            var data = (item, view, itemDescriptorType);
            icon?.TryRefresh(data);
        }

        void SetupSounds() {
            if (!useCustomSound) {
                hoverSound = World.Services.Get<CommonReferences>().AudioConfig.ButtonSelectedSound;
            }

            if (!hoverSound.IsNull) {
                OnHoverStarted += () => FMODManager.PlayOneShot(hoverSound);
            }
        }

        public void NotifyClick() {
            selection.Select();
        }
        
        public void ForceUnselect() {
            selection.ForceUnselect();
        }

        public void NotifyHover() {
            selection.NotifyHovered();
        }

        public void ResetHoveredState() {
            selection.ResetHoveredState();
        }

        public void ForceRefresh(Item item) {
            selection.ForceRefresh(item);
        }

        public void RefreshSelection(Item item) {
            selection.TryRefresh((item, null, ItemDescriptorType.ExistingItem));
        }

        public void SetIconActive(bool active) => icon?.SetExternalVisibility(active);
        public void SetIconMaterial(Material material) => icon?.SetMaterial(material);
        public void SetBackgroundActive(bool active) => mainBackground?.SetExternalVisibility(active);
        public void SetQuantityActive(bool active) => quantity?.SetExternalVisibility(active);
        public void SetQuantityColor(Color color) => quantity?.SetColor(color);
        public void SetQualityActive(bool active) => quality?.SetExternalVisibility(active);
        public void SetEquippedActive(bool active) => equipped?.SetExternalVisibility(active);
        public void SetInLoadoutActive(bool active) => inLoadout?.SetExternalVisibility(active);
        public void SetWeightActive(bool active) => weight?.SetExternalVisibility(active);
        public void SetEmptyActive(bool active) => empty?.SetExternalVisibility(active);
        public void SetSelectionActive(bool active) => selection?.SetExternalVisibility(active);
        
        public event Action OnSelected {
            add => selection.OnSelect += value;
            remove => selection.OnSelect -= value;
        }
        
        public event Action OnDeselected {
            add => selection.OnDeselect += value;
            remove => selection.OnDeselect -= value;
        }
        
        public event Action OnHoverStarted {
            add => selection.OnHoverStarted += value;
            remove => selection.OnHoverStarted -= value;
        }
        public event Action OnHoverEnded {
            add => selection.OnHoverEnded += value;
            remove => selection.OnHoverEnded -= value;
        }
        
        public void SetVisibilityConfig(in VisibilityConfig config) {
            SetIconActive(config.icon);
            SetBackgroundActive(config.mainBackground);
            SetQuantityActive(config.quantity);
            SetQualityActive(config.quality);
            SetEquippedActive(config.equipped);
            SetInLoadoutActive(config.inLoadout);
            SetWeightActive(config.weight);
            SetEmptyActive(config.empty);
            SetSelectionActive(config.selection);
        }
        
        public struct VisibilityConfig {
            public bool icon;
            public bool mainBackground;
            [UnityEngine.Scripting.Preserve] public bool level;
            public bool quantity;
            public bool quality;
            public bool equipped;
            public bool inLoadout;
            public bool weight;
            public bool empty;
            public bool selection;
            
            public static VisibilityConfig All => new() {
                icon = true,
                mainBackground = true,
                level = true,
                quantity = true,
                quality = true,
                equipped = true,
                inLoadout = true,
                weight = true,
                empty = true,
                selection = true
            };
            
            public static VisibilityConfig Equipment => new() {
                icon = true,
                mainBackground = true,
                level = true,
                quantity = true,
                quality = true,
                weight = true,
                empty = true,
                selection = true
            };
            
            public static VisibilityConfig Tooltip => new() {
                icon = true,
                mainBackground = true,
                quality = true,
                equipped = true,
                level = true
            };
            
            public static VisibilityConfig QuickWheel => new() {
                icon = true,
                mainBackground = true,
                quality = true,
                selection = true
            };
            
            public static VisibilityConfig Crafting => new() {
                icon = true,
                mainBackground = true,
                quantity = true,
                quality = true,
                selection = true
            };
            
            public static VisibilityConfig GearUpgrade => new() {
                icon = true,
                mainBackground = true,
                quantity = true,
                quality = true,
                selection = false
            };
            
            public static VisibilityConfig OnlyEmpty => new() { empty = true };
            public static VisibilityConfig OnlyIcon => new() { icon = true };
        }
    }
}
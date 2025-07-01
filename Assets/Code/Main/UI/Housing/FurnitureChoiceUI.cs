using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Housing;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.UI.Housing {
    public partial class FurnitureChoiceUI : Element<IModel>, IWithRecyclableView {
        public sealed override bool IsNotSaved => true;

        readonly FurnitureSlotBase _furnitureSlot;
        readonly Type _viewType;
        readonly Transform _parentTransform;
        FurnitureVariant _variant;
        
        public int Index { get; }
        public string DisplayName => _variant.FurnitureName;
        public ShareableSpriteReference Icon => _variant.FurnitureIcon;

        readonly EventReference _audioHover = CommonReferences.Get.AudioConfig.ButtonSelectedSound;
        readonly EventReference _audioButtonClick = CommonReferences.Get.AudioConfig.ButtonClickedSound;
        readonly EventReference _audioAccept = CommonReferences.Get.AudioConfig.ButtonAcceptSound;
        readonly EventReference _lightNegativeFeedback = CommonReferences.Get.AudioConfig.LightNegativeFeedbackSound;
        
        public new static class Events {
            public static readonly Event<FurnitureChoiceUI, FurnitureChoiceUI> OnFurnitureVariantHoverStarted = new(nameof(OnFurnitureVariantHoverStarted));
            public static readonly Event<FurnitureChoiceUI, FurnitureChoiceUI> OnFurnitureVariantHoverEnded = new(nameof(OnFurnitureVariantHoverEnded));
            public static readonly Event<FurnitureChoiceUI, FurnitureChoiceUI> OnFurnitureVariantUnlocked = new(nameof(OnFurnitureVariantUnlocked));
            public static readonly Event<FurnitureChoiceUI, FurnitureChoiceUI> OnFurnitureVariantChanged = new(nameof(OnFurnitureVariantChanged));
        }
        
        public FurnitureChoiceUI(FurnitureSlotBase furnitureSlot, FurnitureVariant variant, int variantIdx, Type viewType, Transform parentTransform) {
            _furnitureSlot = furnitureSlot;
            Index = variantIdx;
            _variant = variant;
            _viewType = viewType;
            _parentTransform = parentTransform;
        }

        protected override void OnFullyInitialized() {
            World.SpawnView(this, _viewType, true, true, _parentTransform);
        }

        public void TriggerChoiceHovered(bool hover) {
            if (hover) {
                FMODManager.PlayOneShot(_audioHover);
                this.Trigger(Events.OnFurnitureVariantHoverStarted, this);
            } else {
                this.Trigger(Events.OnFurnitureVariantHoverEnded, this);
            }
        }
        
        public void Select() {
            HandleAudioFeedback();
            Use();
        }

        public void Use() {
            FMODManager.PlayOneShot(_audioAccept);
            _furnitureSlot.TryToSpawnFurnitureVariant(_variant.FurnitureVariantTemplate, true);
            this.Trigger(Events.OnFurnitureVariantChanged, this);
        }
        
        void HandleAudioFeedback() {
            if (IsVariantUsed()) {
                FMODManager.PlayOneShot(_audioButtonClick);
            }
        }
        
        public bool IsVariantUsed() => _furnitureSlot.CurrentFurnitureTemplate == _variant.FurnitureVariantTemplate;
    }
}
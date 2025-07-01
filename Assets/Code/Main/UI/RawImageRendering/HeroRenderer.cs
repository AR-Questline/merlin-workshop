using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Main.UI.HeroCreator;
using Awaken.TG.Main.UI.HeroCreator.ViewComponents;
using Awaken.TG.Main.UI.HeroRendering;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.UI.RawImageRendering {
    [SpawnsView(typeof(VHeroRenderer))]
    public partial class HeroRenderer : HeroRendererBase {
        // === Fields
        int _mainCameraCullingMask;
        
        // === Properties
        public override uint? LightRenderLayerMask => LightRenderLayers.EnvironmentUIMask;
        public override int? WeaponLayer => RenderLayers.UI;
        public Camera Camera => View<VHeroRenderer>().Camera;

        // === Initialization
        public HeroRenderer(BodyFeatures features = null, bool useLoadoutAnimations = true) : base(useLoadoutAnimations) {
            _features = features;
        }
        
        protected override void OnFullyInitialized() {
            AddElement(new GameCameraUIOverride());
            base.OnFullyInitialized();
        }
        
        // === Public API
        public void SetHeroVisibility(bool visibility) {
            View<VHeroRenderer>().SetExternalHeroVisibility(visibility);
            
            if (visibility) {
                View<VRotator>().RecalculateRotatableArea().Forget();
            }
        }
        
        public void SetViewTarget(Target target) {
            View<VHeroRenderer>().SetViewTarget(target);
        }
        
        public void SetViewTargetInstant(Target target) {
            View<VHeroRenderer>().SetViewTargetInstant(target);
        }
        
        public void SetViewTarget(EquipmentSlotType equipmentSlotType) {
            View<VHeroRenderer>().SetViewTarget(equipmentSlotType);
        }
        
        public void SetupRotatableArea(RotatableObject rotatable) {
            View<VRotator>().SetupRotatableArea(rotatable);
        }
        
        public void SetRotatableState(bool state) {
            View<VHeroRenderer>().SetRotatableState(state);
        }

        public void ShowForegroundQuad() {
            View<VHeroRenderer>().ShowForegroundQuad();
        }
        
        public void HideForegroundQuad() {
            View<VHeroRenderer>().HideForegroundQuad();
        }
        
        public void FadeForegroundQuad(float targetAlpha, float fadeTime, float fadeDelay) {
            View<VHeroRenderer>().FadeForegroundQuad(targetAlpha, fadeTime, fadeDelay);
        }
        
        // === Helpers
        public enum Target {
            Hero,
            Head,
            Hand,
            Legs,
            Feet,
            Chest,
            Back,
            CCBody,
            CCHead,
            HeroUIInventory,
            HeroUIStatus,
            HeroUIStatsSummary,
            OutOfScreen
        }
    }
}

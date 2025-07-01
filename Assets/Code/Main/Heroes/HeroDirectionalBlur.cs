using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroDirectionalBlur : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        VolumeWrapper _postProcess;
        DirectionalBlur _volumeComponent;
        Vector3 _blurVelocity;
        Camera _heroCamera;

        Vector3 _targetBlurVelocity;
        float _blurVelocityChangeSpeed;
        
        Hero Hero => ParentModel;
        
        protected override void OnInitialize() {
            _postProcess = World.Services.Get<SpecialPostProcessService>().VolumeDirectionalBlur;
            if (!_postProcess.TryGetVolumeComponent(out _volumeComponent)) {
                Log.Important?.Error("HeroDirectionalBlur: VolumeDirectionalBlur has no DirectionalBlur component");
            }
            
            _postProcess.SetWeight(1.0f, 0.5f);
            
            ParentModel.AfterFullyInitialized(AfterParentFullyInitialized);
        }
        
        void AfterParentFullyInitialized() {
            _heroCamera = Hero.VHeroController.MainCamera;
            if (_volumeComponent != null) {
                ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            }
        }
        
        public void SetBlurVelocity(Vector3 velocity, float time = 0.0f) {
            _targetBlurVelocity = velocity;
            if (time == 0.0f) {
                _blurVelocity = velocity;
            } else {
                _blurVelocityChangeSpeed = (_blurVelocity - velocity).magnitude / time;
            }
        }

        void OnUpdate(float deltaTime) {
            if (_volumeComponent.active) {
                UpdateBlurVelocityValue(deltaTime);
                UpdateVolumeComponentValues();
            }
        }

        void UpdateBlurVelocityValue(float deltaTime) {
            _blurVelocity = Vector3.MoveTowards(_blurVelocity, _targetBlurVelocity, _blurVelocityChangeSpeed * deltaTime);
        }
        
        void UpdateVolumeComponentValues() {
            var screenPos = _heroCamera.WorldToScreenPoint(_heroCamera.transform.position + _blurVelocity);
            _volumeComponent.center.value = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);

            _volumeComponent.intensity.value = _blurVelocity.magnitude;
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel?.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            _postProcess.SetWeight(0.0f, 0.25f);
        }
    }
}
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.Main.Rendering;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroDirectionalBlur : Element<IModel> {
        public sealed override bool IsNotSaved => true;

        readonly Transform _followObject;
        readonly VolumeWrapper _postProcess;
        readonly float _onDiscardFadeOutSpeed;
        
        DirectionalBlur _volumeComponent;
        ChromaticAberration _chromaticAberration;
        Vector3 _blurVelocity;
        Camera _heroCamera;

        Vector3 _targetBlurVelocity;
        float _blurVelocityChangeSpeed;

        public HeroDirectionalBlur(VolumeWrapper postProcess, Transform followObject, float onDiscardFadeOutSpeed = 0.25f) {
            _postProcess = postProcess;
            _followObject = followObject;
            _onDiscardFadeOutSpeed = onDiscardFadeOutSpeed;
        }
        
        protected override void OnInitialize() {
            if (!_postProcess.TryGetVolumeComponent(out _volumeComponent)) {
                Log.Important?.Error("HeroDirectionalBlur: VolumeDirectionalBlur has no DirectionalBlur component");
            }
            _postProcess.TryGetVolumeComponent(out _chromaticAberration);
            _postProcess.SetWeightInstant(1.0f);
            SetBlurVelocity(Vector3.forward);
            ParentModel.AfterFullyInitialized(AfterParentFullyInitialized);
        }
        
        void AfterParentFullyInitialized() {
            _heroCamera = World.Only<CameraStateStack>().MainCamera;
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
            var screenPos = _heroCamera.WorldToScreenPoint(_followObject.position + _blurVelocity);
            _volumeComponent.center.value = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);

            _volumeComponent.intensity.value = _blurVelocity.magnitude;
            if (_chromaticAberration) {
                _chromaticAberration.intensity.value = _blurVelocity.magnitude;
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel?.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            _postProcess.SetWeight(0.0f, _onDiscardFadeOutSpeed);
        }
    }
}
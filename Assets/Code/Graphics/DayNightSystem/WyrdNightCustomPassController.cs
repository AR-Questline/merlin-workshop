using System.Linq;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.DayNightSystem {
    public class WyrdNightCustomPassController : WyrdNightControllerBase {
        [SerializeField] string materialProperty = "_Intensity";
        [SerializeField] CustomPassType customPassType = CustomPassType.WyrdEdge;
        bool _createdLocalInstance;

        CustomPassVolume _customPassVolume;
        int _propertyID;
        Material _targetMaterialInstance;

        protected override void OnAwake() {
            base.OnAwake();
            _customPassVolume = GetComponent<CustomPassVolume>();

            if (_customPassVolume == null) {
                Debug.LogError($"No CustomPassVolume component found on {gameObject.name}" + gameObject.HierarchyPath());
                enabled = false;
                return;
            }
            
            if (!TryGetTargetMaterial())
                return;

            ApplyEffect(EnabledValue);
        }

        bool TryGetTargetMaterial() {
            switch (customPassType) {
                case CustomPassType.FullScreen:
                    HandleFullScreenPass();
                    break;

                case CustomPassType.WyrdEdge:
                    HandleWyrdEdgePass();
                    break;
            }
            
            if (_targetMaterialInstance is null) {
                Debug.LogError($"Failed to initialize material instance on {gameObject.name}." + gameObject.HierarchyPath());
                enabled = false;
                return false;
            }

            _propertyID = Shader.PropertyToID(materialProperty);

            if (!_targetMaterialInstance.HasProperty(_propertyID)) {
                Debug.LogError($"Material instance does not have property '{materialProperty}' on {gameObject.name}.");
                enabled = false;
                return false;
            }
            return true;
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (_customPassVolume != null && _targetMaterialInstance == null) {
                if (TryGetTargetMaterial()) {
                    ApplyEffect(CurrentValue);
                }
            }
        }

        protected override void OnDestroy() {
            if (_createdLocalInstance && _targetMaterialInstance != null) {
                CoreUtils.Destroy(_targetMaterialInstance);
            }
            base.OnDestroy();
        }

        void HandleFullScreenPass() {
            FullScreenCustomPass fullScreenPass =
                _customPassVolume.customPasses.OfType<FullScreenCustomPass>().FirstOrDefault();

            if (fullScreenPass == null || fullScreenPass.fullscreenPassMaterial == null) {
                Debug.LogError("FullScreenCustomPass or its material not found." + gameObject.HierarchyPath());
                return;
            }
            
            _targetMaterialInstance = new Material(fullScreenPass.fullscreenPassMaterial);
            _targetMaterialInstance.name = $"{fullScreenPass.fullscreenPassMaterial.name}_Instance";

            fullScreenPass.fullscreenPassMaterial = _targetMaterialInstance;
            _createdLocalInstance = true;
        }

        void HandleWyrdEdgePass() {
            HeroWyrdNightEdge wyrdPass = _customPassVolume.customPasses.OfType<HeroWyrdNightEdge>().FirstOrDefault();

            if (wyrdPass == null) {
                Debug.LogError("HeroWyrdNightEdge pass not found in CustomPassVolume." + gameObject.HierarchyPath());
                return;
            }

            if (wyrdPass.sourceMaterial == null) {
                Debug.LogError("HeroWyrdNightEdge: 'sourceMaterial' is not assigned in Custom Pass Inspector!" + gameObject.HierarchyPath());
                return;
            }
            
            _targetMaterialInstance = wyrdPass.GetRuntimeMaterial();
            _createdLocalInstance = false; 
        }

        protected override void ApplyEffect(float value) {
            if (!Application.isPlaying) return;
            _targetMaterialInstance.SetFloat(_propertyID, value);
        }
    }
    
    public enum CustomPassType : byte {
        FullScreen,
        WyrdEdge
    }
}
using System.Linq;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.DayNightSystem {
    public class WyrdNightCustomPassController : WyrdNightControllerBase {
        [SerializeField]
        string materialProperty = "_Alpha";
        [SerializeField]
        CustomPassType customPassType = CustomPassType.FullScreen;
        
        CustomPassVolume _customPassVolume;
        Material _customPassMaterial;
        int _propertyID;
        
        protected override void OnAwake() {
            _customPassVolume = gameObject.GetComponent<CustomPassVolume>();

            if (_customPassVolume == null) {
                Debug.LogError("No CustomPassVolume component found on " + gameObject.HierarchyPath());
                enabled = false;
                return;
            }

            switch (customPassType) {
                case CustomPassType.FullScreen: {
                    var fullScreenCustomPass = _customPassVolume.customPasses.OfType<FullScreenCustomPass>().FirstOrDefault();
                    if (fullScreenCustomPass != null) {
                        _customPassMaterial = fullScreenCustomPass.fullscreenPassMaterial;
                    } else {
                        Debug.LogError("FullScreenCustomPass not found in CustomPassVolume custom passes.");
                    }
                    break;
                }
                case CustomPassType.WyrdEdge: {
                    var wyrdNightEdgeCustomPass = _customPassVolume.customPasses.OfType<HeroWyrdNightEdge>().FirstOrDefault();
                    if (wyrdNightEdgeCustomPass != null) {
                        _customPassMaterial = wyrdNightEdgeCustomPass.fullScreenMaterial;
                    } else {
                        Debug.LogError("HeroWyrdNightEdge not found in CustomPassVolume custom passes.");
                    }
                    break;
                }
            }

            if (_customPassMaterial == null) {
                Debug.LogError("No material found for custom pass on " + gameObject.HierarchyPath());
                enabled = false;
                return;
            }
            _propertyID = Shader.PropertyToID(materialProperty);
            
            if (!_customPassMaterial.HasProperty(_propertyID)) {
                Debug.LogError("Material does not have property " + materialProperty + " on " + gameObject.HierarchyPath());
                enabled = false;
                return;
            }
            ApplyEffect(EnabledValue);
        }

        protected override void ApplyEffect(float value) {
            if (!Application.isPlaying) return;
            _customPassMaterial.SetFloat(_propertyID, value);
        }

        enum CustomPassType : byte {
            FullScreen,
            WyrdEdge,
        }
    }
}
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Graphics.DayNightSystem {
    public class WyrdnightMaterialController : WyrdNightControllerBase {
        [SerializeField] string materialProperty = "_Transition";
        [SerializeField] bool disableRendererWhenZero = true;

        Material _materialInstance;
        MeshRenderer _meshRenderer;
        int _propertyID;
        
        protected override void OnAwake() {
            _meshRenderer = GetComponent<MeshRenderer>();
            
            if (_meshRenderer == null) {
                Log.Important?.Error($"Mesh renderer is null on {gameObject.HierarchyPath()}");
                enabled = false;
                return;
            }
            
            _materialInstance = _meshRenderer.material;
            
            if (_materialInstance == null) {
                Log.Important?.Error($"Material instance is null on {gameObject.HierarchyPath()}");
                enabled = false;
                return;
            }
            
            _propertyID = Shader.PropertyToID(materialProperty);
            
            if (!_materialInstance.HasProperty(_propertyID)) {
                Log.Important?.Error($"Material {_materialInstance.name} does not have property {materialProperty} on {gameObject.HierarchyPath()}");
                enabled = false;
                return;
            }
            
            ApplyEffect(EnabledValue);
        }

        protected override void ApplyEffect(float value) {
            _materialInstance.SetFloat(_propertyID, value);
            if (disableRendererWhenZero) {
                _meshRenderer.enabled = value > 0;
            }
        }

        protected override IBackgroundTask OnDiscard() {
            Destroy(_materialInstance);
            return base.OnDiscard();
        }

        void Reset() {
            gameObject.GetOrAddComponent<MeshRenderer>();
        }
    }
}
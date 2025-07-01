using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.DayNightSystem {
    [RequireComponent(typeof(DayNightSystem))]
    public class WyrdnightSkyboxController : WyrdNightControllerBase {
        [SerializeField] string materialProperty = "_Space_Wyrdness";
        [ShowInInspector, ReadOnly] Material _customSkyboxInstance;
        [ShowInInspector, ReadOnly] int _propertyID;

        void Start() {
            _customSkyboxInstance = GetComponent<DayNightSystem>().SkyboxInstance;
            if (_customSkyboxInstance == null) {
                Log.Critical?.Error("No skybox material found on DayNightSystem: " + gameObject.HierarchyPath());
                Discard();
                return;
            }
            _propertyID = Shader.PropertyToID(materialProperty);
            if (!_customSkyboxInstance.HasProperty(_propertyID)) {
                Log.Critical?.Error("Material property not found on skybox material: " + materialProperty + " on " + gameObject.HierarchyPath());
                Discard();
                return;
            }
        }

        protected override void ApplyEffect(float value) {
            _customSkyboxInstance?.SetFloat(_propertyID, value);
        }
    }
}
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    public class MaterialStateAdjuster : MonoBehaviour {
        [SerializeField] int currentState;
        [FormerlySerializedAs("propertiesToModify")] [SerializeField] MaterialStateSettings[] materialStates = Array.Empty<MaterialStateSettings>();
        [SerializeField] Renderer target;

        [ShowInInspector, ReadOnly] Material[] _sharedMaterialsBackup;
        [ShowInInspector, ReadOnly] List<Material> _instancedMaterials = new();

        void Awake() {
            if (target == null) {
                target = GetComponentsInChildren<Renderer>()[0];
            }

            _sharedMaterialsBackup = target.sharedMaterials;
            for (var index = 0; index < materialStates.Length; index++) {
                materialStates[index].Init();
            }
        }

        [Button]
        public void Clear() {
            target.sharedMaterials = _sharedMaterialsBackup;
            currentState = -1;
            foreach (var mat in _instancedMaterials) {
                Destroy(mat);
            }

            _instancedMaterials.Clear();
        }

        [Button]
        public void SetState(int stateId) {
            int clamp = Mathf.Clamp(stateId, 0, materialStates.Length - 1);
            if (currentState != clamp) {
                currentState = clamp;
                ApplyCurrentState();
            }
        }

        [ContextMenu(nameof(ApplyCurrentState))]
        void ApplyCurrentState() {
            if (_instancedMaterials.Count == 0) {
                target.GetMaterials(_instancedMaterials);
            }
            foreach (IShaderPropertySetter paramSetting in materialStates[currentState].shaderPropertySetters) {
                paramSetting.ApplyTo(_instancedMaterials[paramSetting.MaterialIDInRenderer]);
            }
        }
    }
    
    // === Serializable material configs
    [Serializable]
    public class MaterialStateSettings {
        [SerializeReference]
        public List<IShaderPropertySetter> shaderPropertySetters;

        public void Init() {
            for (var index = 0; index < shaderPropertySetters.Count; index++) {
                shaderPropertySetters[index].Init();
            }
        }
    }

    public interface IShaderPropertySetter {
        void Init();
        void ApplyTo(Material mat);
        int MaterialIDInRenderer { get; }
    }
    [Serializable]
    public class FloatShaderProperty : IShaderPropertySetter {
        [SerializeField] string shaderProperty;
        [SerializeField] int materialNumberInRenderer;
        [SerializeField] float value;

        [HideInEditorMode, ReadOnly]
        int _propertyID;

        public void Init() {
            _propertyID = Shader.PropertyToID(shaderProperty);
        }

        public void ApplyTo(Material mat) {
            mat.SetFloat(_propertyID, value);
        }

        public int MaterialIDInRenderer => materialNumberInRenderer;
    }
    [Serializable]
    public class ColorShaderProperty : IShaderPropertySetter {
        [SerializeField] string shaderProperty;
        [SerializeField] int materialNumberInRenderer;
        [SerializeField] Color value;

        [HideInEditorMode, ReadOnly]
        int _propertyID;

        public void Init() {
            _propertyID = Shader.PropertyToID(shaderProperty);
        }

        public void ApplyTo(Material mat) {
            mat.SetColor(_propertyID, value);
        }

        public int MaterialIDInRenderer => materialNumberInRenderer;
    }
}
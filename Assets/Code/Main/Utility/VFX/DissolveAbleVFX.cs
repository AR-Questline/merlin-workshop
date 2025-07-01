using System;
using Awaken.ECS.DrakeRenderer.Utilities;
using Awaken.TG.Assets;
using Awaken.Utility.Collections;
using Awaken.Utility.SerializableTypeReference;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Utility.VFX {
    /// <summary>
    /// Marker script for renderers that are available to be dissolved.
    /// </summary>
    public class DissolveAbleVFX : MonoBehaviour, IDissolveAble {
        [SerializeField, BoxGroup("Type Setup")] bool isWeapon;
        [SerializeField, BoxGroup("Type Setup")] bool isCloth = true;
        [SerializeField] bool allowCustomPropertyModification;
        [SerializeField] VfxModification[] onDissolve = Array.Empty<VfxModification>();
        [SerializeField] VfxModification[] onRestore = Array.Empty<VfxModification>();
        [SerializeField] VisualEffect[] vfxGraphs = Array.Empty<VisualEffect>();
        
        IDissolveAbleDissolveController _dissolveController;
        bool _isInDissolvableState;

        public bool IsInDissolvableState => _isInDissolvableState;
        public bool IsWeapon => isWeapon;
        public bool IsCloth => isCloth;

        void Awake() {
            Init(true);
        }

        public void Init(bool fromStart = false) {
            if (vfxGraphs.IsEmpty()) {
                vfxGraphs = GetComponentsInChildren<VisualEffect>(true);
            }
        }
        
        void OnDestroy() {
            if (_dissolveController != null) {
                _dissolveController.RemoveRenderer(this);
                _dissolveController = null;
            }

            _isInDissolvableState = false;
        }

        public void AssignController(IDissolveAbleDissolveController controller) {
            _dissolveController = controller;
        }

        public void SetCustomDissolveAbleMaterials(Material[] materials, ARAssetReference[] materialRefs, bool forceReplaceOnNonRepreceableDAR = false) { }

        public void ChangeToDissolveAble() {
            if (_isInDissolvableState) {
                return;
            }

            foreach (var vfx in vfxGraphs) {
                foreach (var modification in onDissolve) {
                    modification.Apply(vfx);
                }
            }
            _isInDissolvableState = true;
        }

        public void RestoreToOriginal() {
            if (!_isInDissolvableState) {
                return;
            }

            foreach (var vfx in vfxGraphs) {
                foreach (var modification in onRestore) {
                    modification.Apply(vfx);
                }
            }
            _isInDissolvableState = false;
        }

        public void InitPropertyModification(SerializableTypeReference serializedType, float value) { }

        public void UpdateProperty(SerializableTypeReference serializedType, float value) {
            if (!allowCustomPropertyModification) {
                return;
            }
            var shaderNameId = MaterialOverrideUtils.GetPropertyID(serializedType);
            foreach (var vfx in vfxGraphs) {
                vfx.SetFloat(shaderNameId, value);
            }
        }

        public void FinishPropertyModification(SerializableTypeReference serializedType) { }
    }

    [Serializable]
    public struct VfxModification {
        public ModificationType type;
        public string name;
        [ShowIf(nameof(ShowFloat))] public float floatValue;
        [ShowIf(nameof(ShowBool))] public bool boolValue;
        
        bool ShowFloat => type == ModificationType.SetFloat;
        bool ShowBool => type == ModificationType.SetBool;

        public enum ModificationType {
            SendEvent,
            SetFloat,
            SetBool,
        }
        
        public void Apply(VisualEffect vfx) {
            switch (type) {
                case ModificationType.SendEvent:
                    vfx.SendEvent(name);
                    break;
                case ModificationType.SetFloat:
                    vfx.SetFloat(name, floatValue);
                    break;
                case ModificationType.SetBool:
                    vfx.SetBool(name, boolValue);
                    break;
            }
        }
    }
}
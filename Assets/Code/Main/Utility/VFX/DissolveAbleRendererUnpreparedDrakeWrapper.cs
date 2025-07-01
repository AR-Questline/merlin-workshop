using System;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Utilities;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Collections;
using Awaken.Utility.SerializableTypeReference;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    /// <summary>
    /// Marker script for drake renderers that are not available yet to be dissolved.
    /// </summary>
    public class DissolveAbleRendererUnpreparedDrakeWrapper : IDissolveAbleRendererWrapper {
        readonly DissolveAbleRenderer _dar;
        ARUnsafeList<MaterialOverrideData> _overrideDatas;

        public Material[] InstancedMaterialsForExternalModifications => Array.Empty<Material>();
        public bool InDissolvableState { get; private set; }
        public DissolveAbleRendererUnpreparedDrakeWrapper(DissolveAbleRenderer dar) {
            _dar = dar;
        }
        
        public void Init() {
            _overrideDatas = new ARUnsafeList<MaterialOverrideData>(4, Allocator.Persistent);
            TryChangeToDrakeWrapper();
        }

        public void Destroy() {
            _overrideDatas.Dispose();
        }

        public void ChangeToDissolveAble() {
            InDissolvableState = true;
            TryChangeToDrakeWrapper();
        }

        public void RestoreToOriginalMaterials() {
            InDissolvableState = false;
            TryChangeToDrakeWrapper();
        }
        
        public void InitPropertyModification(SerializableTypeReference serializedType, float value) {
            var materialOverride = new MaterialOverrideData(TypeManager.GetTypeIndex(serializedType), value);
            _overrideDatas.Add(materialOverride);

            TryChangeToDrakeWrapper();
        }

        public void UpdateProperty(SerializableTypeReference serializedType, float value) {
            var typeIndex = TypeManager.GetTypeIndex(serializedType);
            var existingIndex = _overrideDatas.FindIndexOf(new MaterialOverrideDataByTypeEquality(typeIndex));
            if (existingIndex != -1) {
                _overrideDatas[existingIndex] = new MaterialOverrideData(typeIndex, value);
            } else {
                _overrideDatas.Add(new MaterialOverrideData(typeIndex, value));
            }

            TryChangeToDrakeWrapper();
        }

        public void FinishPropertyModification(SerializableTypeReference serializedType) {
            var typeIndex = TypeManager.GetTypeIndex(serializedType);
            var existingIndex = _overrideDatas.FindIndexOf(new MaterialOverrideDataByTypeEquality(typeIndex));
            if (existingIndex != -1) {
                _overrideDatas.RemoveAtSwapBack(existingIndex);
            }

            TryChangeToDrakeWrapper();
        }

        void TryChangeToDrakeWrapper() {
            if (HasEntitiesAccess(out var linkedEntitiesAccess)) {
                var drakeWrapper = new DissolveAbleRendererDrakeWrapper(_dar, linkedEntitiesAccess);
                
                if (_overrideDatas.Length > 0) {
                    var pack = new MaterialsOverridePack(_overrideDatas.AsUnsafeSpan());
                    MaterialOverrideUtils.ApplyMaterialOverrides(linkedEntitiesAccess, pack);
                }
                
                _dar.ForceWrapperChange(drakeWrapper);
                if (InDissolvableState) {
                    _dar.ChangeToDissolveAble();
                }
            }
        }
        
        bool HasEntitiesAccess(out LinkedEntitiesAccess linkedEntitiesAccess) {
            linkedEntitiesAccess = _dar.GetComponent<LinkedEntitiesAccess>();
            return linkedEntitiesAccess is not (null or { LinkedEntities: { IsCreated: false } });
        }

        readonly struct MaterialOverrideDataByTypeEquality : IEquatable<MaterialOverrideData> {
            readonly TypeIndex _typeIndex;

            public MaterialOverrideDataByTypeEquality(TypeIndex typeIndex) {
                this._typeIndex = typeIndex;
            }

            public bool Equals(MaterialOverrideData other) {
                return _typeIndex.Equals(other.typeIndex);
            }
        }
    }
}
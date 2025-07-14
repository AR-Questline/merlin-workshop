using System;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Utilities;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.SerializableTypeReference;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeAnimatedPropertiesOverrideController : MonoBehaviour, IDrakeMeshRendererBakingModificationStep, IDrakeMeshRendererBakingStep {
        static readonly IWithUnityRepresentation.Options RequiresEntitiesAccess = new IWithUnityRepresentation.Options {
            requiresEntitiesAccess = true
        };

        [SerializeField] AuthoringDataMaterialData[] overrides;
        [SerializeField] float duration = 1f;
        [SerializeField] bool animateOnEnable;

        bool _tweening;
        bool _progressForward;
        float _currentPercentage;

        UnsafeArray<MaterialOverrideData> _runtimeOverrides;
        UnsafeArray<float2> _fromToFloatValues;
        UnsafeArray<float4x2> _fromToFloat4Values;
        UnsafeArray<FixedString128Bytes> _materialKeys;

        void Awake() {
            Init();
        }

        void OnEnable() {
            if (animateOnEnable)
                StartForward();
        }

        void OnDestroy() {
            _runtimeOverrides.Dispose();
            _fromToFloatValues.Dispose();
            _fromToFloat4Values.Dispose();
            _materialKeys.Dispose();
        }

        [Button]
        public void StartForward() {
            _tweening = true;
            _progressForward = true;
            Init();
        }

        [Button]
        public void StartBackward() {
            _tweening = true;
            _progressForward = false;
            Init();
        }

        [Button]
        public void Stop() {
            _tweening = false;
            _currentPercentage = 0f;
        }

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            drakeMeshRenderer.SetUnityRepresentation(RequiresEntitiesAccess);
        }

        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity, in LodGroupSerializableData lodGroupData, in DrakeMeshMaterialComponent drakeMeshMaterialComponent, Entity entity, ref EntityCommandBuffer ecb) {
            Init();

            var materialOverridePack = new MaterialsOverridePack(_runtimeOverrides, _materialKeys);
            MaterialOverrideUtils.AddMaterialOverrides(materialOverridePack, drakeMeshMaterialComponent, entity, ref ecb);
        }

        void Init() {
            var floatCount = 0u;
            var float4Count = 0u;
            var overridesCount = (uint) overrides.Length;
            for (uint i = 0; i < overridesCount; i++) {
                floatCount += (uint)overrides[i].floatPropertyData.Length;
                float4Count += (uint)overrides[i].colorPropertyData.Length;
            }
            var allCount = floatCount + float4Count;

            _runtimeOverrides = new UnsafeArray<MaterialOverrideData>(allCount, Allocator.Persistent);
            _fromToFloatValues = new UnsafeArray<float2>(floatCount, Allocator.Persistent);
            _fromToFloat4Values = new UnsafeArray<float4x2>(float4Count, Allocator.Persistent);
            _materialKeys = new UnsafeArray<FixedString128Bytes>(allCount, Allocator.Persistent);

            var floatIndex = 0u;
            var float4Index = 0u;
            for (uint i = 0; i < overridesCount; i++) {
                var materialKey = new FixedString128Bytes(overrides[i].material.AssetGUID);

                for(uint j = 0; j < overrides[i].floatPropertyData.Length; j++) {
                    _materialKeys[floatIndex] = materialKey;
                    _fromToFloatValues[floatIndex] = new float2(overrides[i].floatPropertyData[j].fromValue, overrides[i].floatPropertyData[j].toValue);
                    var currentValue = math.lerp(_fromToFloatValues[floatIndex].x, _fromToFloatValues[floatIndex].y, _currentPercentage);
                    var typeIndex = TypeManager.GetTypeIndex(overrides[i].floatPropertyData[j].serializedType);
                    _runtimeOverrides[floatIndex] = new MaterialOverrideData(typeIndex, currentValue);
                    ++floatIndex;
                }

                for(uint j = 0; j < overrides[i].colorPropertyData.Length; j++) {
                    var realIndex = floatCount + float4Index;
                    _materialKeys[realIndex] = materialKey;
                    _fromToFloat4Values[float4Index] = overrides[i].colorPropertyData[j].GetColorsAsFloat4();
                    var currentValue = math.lerp(_fromToFloat4Values[float4Index].c0, _fromToFloat4Values[float4Index].c1, _currentPercentage);
                    var typeIndex = TypeManager.GetTypeIndex(overrides[i].colorPropertyData[j].serializedType);
                    _runtimeOverrides[realIndex] = new MaterialOverrideData(typeIndex, currentValue);
                    ++float4Index;
                }
            }
        }

        void Update() {
            if (!_tweening) {
                return;
            }

            UpdateProgress();

            UpdateOverrides();
        }

        void UpdateProgress() {
            if (_progressForward) {
                _currentPercentage += Time.deltaTime / duration;
                if (_currentPercentage >= 1f) {
                    _currentPercentage = 1f;
                    _tweening = false;
                }
            } else {
                _currentPercentage -= Time.deltaTime / duration;
                if (_currentPercentage <= 0f) {
                    _currentPercentage = 0f;
                    _tweening = false;
                }
            }
        }

        void UpdateOverrides() {
            // Update the values
            var floatValuesLength = _fromToFloatValues.Length;
            for (var i = 0u; i < floatValuesLength; i++) {
                var currentValue = math.lerp(_fromToFloatValues[i].x, _fromToFloatValues[i].y, _currentPercentage);
                var runtimeOverride = new MaterialOverrideData(_runtimeOverrides[i].typeIndex, currentValue);
                _runtimeOverrides[i] = runtimeOverride;
            }
            for (var i = 0u; i < _fromToFloat4Values.Length; i++) {
                var realIndex = floatValuesLength + i;
                var currentValue = math.lerp(_fromToFloat4Values[i].c0, _fromToFloat4Values[i].c1, _currentPercentage);
                var runtimeOverride = new MaterialOverrideData(_runtimeOverrides[realIndex].typeIndex, currentValue);
                _runtimeOverrides[realIndex] = runtimeOverride;
            }

            var access = GetComponent<LinkedEntitiesAccess>();
            if (access == null) {
                //TODO: don't allow for this situation to even happen (currently if Drake is spawned in NPC visual prefab (not location) this happens.
                return;
            }

            var materialOverridePack = new MaterialsOverridePack(_runtimeOverrides, _materialKeys);
            MaterialOverrideUtils.ApplyMaterialOverrides(access, materialOverridePack);
        }

        [Serializable]
        struct AuthoringDataMaterialData {
            public AssetReferenceT<Material> material;
            public FloatPropertyOverrideData[] floatPropertyData;
            public ColorPropertyOverrideData[] colorPropertyData;
        }

        [Serializable]
        struct FloatPropertyOverrideData {
            [MaterialPropertyComponent]
            public SerializableTypeReference serializedType;
            public float fromValue;
            public float toValue;
        }

        [Serializable]
        struct ColorPropertyOverrideData {
            [MaterialPropertyComponent]
            public SerializableTypeReference serializedType;
            [ColorUsage(true, true)]
            public Color fromValue;
            [ColorUsage(true, true)]
            public Color toValue;

            public float4x2 GetColorsAsFloat4() {
                return new float4x2(new float4(fromValue.r, fromValue.g, fromValue.b, fromValue.a),
                    new float4(toValue.r, toValue.g, toValue.b, toValue.a));
            }
        }
    }
}

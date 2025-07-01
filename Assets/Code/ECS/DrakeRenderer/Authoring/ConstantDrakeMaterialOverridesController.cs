using System;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.DrakeRenderer.Utilities;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.SerializableTypeReference;
using Sirenix.OdinInspector;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class ConstantDrakeMaterialOverridesController : MonoBehaviour, IDrakeMeshRendererBakingModificationStep, IDrakeMeshRendererBakingStep {
        static readonly IWithUnityRepresentation.Options RequiresEntitiesAccess = new IWithUnityRepresentation.Options {
            requiresEntitiesAccess = true
        };

        [SerializeField, OnValueChanged("EDITOR_OverridesChanged", true)]
        AuthoringOverrideData[] overrides = Array.Empty<AuthoringOverrideData>();

        UnsafeArray<MaterialOverrideData> _runtimeOverrides;

        void Awake() {
            Init();
        }

        void OnDestroy() {
            _runtimeOverrides.Dispose();
        }

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            drakeMeshRenderer.SetUnityRepresentation(RequiresEntitiesAccess);
        }

        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity, in LodGroupSerializableData lodGroupData, in DrakeMeshMaterialComponent drakeMeshMaterialComponent, Entity entity, ref EntityCommandBuffer ecb) {
            Init();

            var materialOverridePack = new MaterialsOverridePack(_runtimeOverrides);
            MaterialOverrideUtils.AddMaterialOverrides(materialOverridePack, drakeMeshMaterialComponent, entity, ref ecb);
        }

        void Init() {
            if (_runtimeOverrides.IsCreated) {
                return;
            }

            var frame = Time.frameCount;
            var rng = new Random((uint)(frame == 0 ? 69 : frame));
            var runtimeOverrides = new UnsafeList<MaterialOverrideData>(overrides.Length, ARAlloc.Temp);
            for (var i = overrides.Length - 1; i >= 0; i--) {
                if (overrides[i].ToRuntime(out var runtimeData, ref rng)) {
                    runtimeOverrides.Add(runtimeData);
                }
            }
            _runtimeOverrides = runtimeOverrides.ToUnsafeArray(ARAlloc.Persistent);
            runtimeOverrides.Dispose();
        }

#if UNITY_EDITOR
        void EDITOR_OverridesChanged() {
            // there may be additional overrides or some removed
            UnsafeList<MaterialOverrideData> oldOverrides = default;
            if (_runtimeOverrides.IsCreated) {
                oldOverrides = _runtimeOverrides.ToUnsafeList(ARAlloc.Temp);
                _runtimeOverrides.Dispose();
            }
            Init();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                Log.Important?.Error("No default world", gameObject);
                if (oldOverrides.IsCreated) {
                    oldOverrides.Dispose();
                }
                return;
            }
            var entityManager = world.EntityManager;

            var access = GetComponent<LinkedEntitiesAccess>();
            if (access == null) {
                Log.Important?.Error($"There is no {nameof(LinkedEntitiesAccess)} on {gameObject.name}", gameObject);
                if (oldOverrides.IsCreated) {
                    oldOverrides.Dispose();
                }
                return;
            }

            var ecb = new EntityCommandBuffer(ARAlloc.Temp);

            foreach (var entity in access.LinkedEntities) {
                for (var i = 0u; i < _runtimeOverrides.Length; i++) {
                    MaterialOverrideUtils.ApplyMaterialOverride(ref entityManager, entity, _runtimeOverrides[i], ref ecb);

                    if (oldOverrides.IsCreated) {
                        var index = oldOverrides.FindIndexOf(new RuntimeOverrideDataMatch(_runtimeOverrides[i]));
                        if (index != -1) {
                            oldOverrides.RemoveAt(index);
                        }
                    }
                }

                if (oldOverrides.IsCreated) {
                    for (var i = oldOverrides.Length - 1; i >= 0; i--) {
                        ecb.RemoveComponent(entity, oldOverrides[i].ComponentType);
                    }
                    oldOverrides.Dispose();
                }
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }
#endif

        [Serializable]
        unsafe struct AuthoringOverrideData {
            // Serialized data
            [SerializeField, MaterialPropertyComponent]
            SerializableTypeReference serializedType;
            [SerializeField] internal bool useRandom;
            [SerializeField, ReadOnly] internal fixed float data[4];
            [SerializeField, ReadOnly] internal fixed float data2[4];

            public bool ToRuntime(out MaterialOverrideData materialData, ref Random rng) {
                var type = serializedType.Type;
                if (type == null) {
                    materialData = default;
                    return false;
                }

                var typeIndex = TypeManager.GetTypeIndex(serializedType);
                if (typeIndex == TypeIndex.Null) {
                    materialData = default;
                    return false;
                }
                
                if (useRandom) {
                    materialData = new MaterialOverrideData(typeIndex,
                        rng.NextFloat(data[0], data2[0]), rng.NextFloat(data[1], data2[1]),
                        rng.NextFloat(data[2], data2[2]), rng.NextFloat(data[3], data2[3]));
                } else {
                    materialData = new MaterialOverrideData(typeIndex,
                        data[0], data[1], data[2], data[3]);
                }
                return true;
            }

            // === Inspector
#if UNITY_EDITOR
            [ShowInInspector, ShowIf(nameof(IsColor))] public Color ColorValue {
                get => new Color(data[0], data[1], data[2], data[3]);
                set {
                    data[0] = value.r;
                    data[1] = value.g;
                    data[2] = value.b;
                    data[3] = value.a;
                }
            }
            [ShowInInspector, ShowIf(nameof(UseRandomColor))] public Color ColorValue2 {
                get => new Color(data2[0], data2[1], data2[2], data2[3]);
                set {
                    data2[0] = value.r;
                    data2[1] = value.g;
                    data2[2] = value.b;
                    data2[3] = value.a;
                }
            }

            [ShowInInspector, ShowIf(nameof(IsFloat))] public float FloatValue {
                get => data[0];
                set {
                    data[0] = value;
                }
            }

            [ShowInInspector, ShowIf(nameof(UseRandomFloat))] public float FloatValue2 {
                get => data2[0];
                set {
                    data2[0] = value;
                }
            }

            [ShowInInspector, ShowIf(nameof(IsVector2))] public float2 Float2Value {
                get => new float2(data[0], data[1]);
                set {
                    data[0] = value.x;
                    data[1] = value.y;
                }
            }

            [ShowInInspector, ShowIf(nameof(UseRandomVector2))] public float2 Float2Value2 {
                get => new float2(data2[0], data2[1]);
                set {
                    data2[0] = value.x;
                    data2[1] = value.y;
                }
            }

            [ShowInInspector, ShowIf(nameof(IsVector3))] public float3 Float3Value {
                get => new float3(data[0], data[1], data[2]);
                set {
                    data[0] = value.x;
                    data[1] = value.y;
                    data[2] = value.z;
                }
            }

            [ShowInInspector, ShowIf(nameof(UseRandomVector3))] public float3 Float3Value2 {
                get => new float3(data2[0], data2[1], data2[2]);
                set {
                    data2[0] = value.x;
                    data2[1] = value.y;
                    data2[2] = value.z;
                }
            }

            [ShowInInspector, ShowIf(nameof(IsVector4))] public float4 Float4Value {
                get => new float4(data[0], data[1], data[2], data[3]);
                set {
                    data[0] = value.x;
                    data[1] = value.y;
                    data[2] = value.z;
                    data[3] = value.w;
                }
            }

            [ShowInInspector, ShowIf(nameof(UseRandomVector4))] public float4 Float4Value2 {
                get => new float4(data2[0], data2[1], data2[2], data2[3]);
                set {
                    data2[0] = value.x;
                    data2[1] = value.y;
                    data2[2] = value.z;
                    data2[3] = value.w;
                }
            }

            bool IsColor() {
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(Color);
                }
                return false;
            }

            bool UseRandomColor() {
                if (!useRandom) {
                    return false;
                }
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(Color);
                }
                return false;
            }

            bool IsFloat() {
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(float);
                }
                return false;
            }

            bool UseRandomFloat() {
                if (!useRandom) {
                    return false;
                }
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(float);
                }
                return false;
            }

            bool IsVector2() {
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(float2) || field.FieldType == typeof(Vector2);
                }
                return false;
            }

            bool UseRandomVector2() {
                if (!useRandom) {
                    return false;
                }
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(float2) || field.FieldType == typeof(Vector2);
                }
                return false;
            }

            bool IsVector3() {
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(float3) || field.FieldType == typeof(Vector3);
                }
                return false;
            }

            bool UseRandomVector3() {
                if (!useRandom) {
                    return false;
                }
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(float3) || field.FieldType == typeof(Vector3);
                }
                return false;
            }

            bool IsVector4() {
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(float4) || field.FieldType == typeof(Vector4);
                }
                return false;
            }

            bool UseRandomVector4() {
                if (!useRandom) {
                    return false;
                }
                if (TryGetField(out var field)) {
                    return field.FieldType == typeof(float4) || field.FieldType == typeof(Vector4);
                }
                return false;
            }

            bool TryGetField(out System.Reflection.FieldInfo field) {
                var type = serializedType.Type;
                if (type == null) {
                    field = null;
                    return false;
                }
                var fields = type.GetFields();
                if (fields.Length != 1) {
                    field = null;
                    return false;
                }
                field = fields[0];
                return true;
            }
#endif
        }

        readonly struct RuntimeOverrideDataMatch : IEquatable<MaterialOverrideData> {
            public readonly TypeIndex typeIndex;

            public RuntimeOverrideDataMatch(in MaterialOverrideData data) {
                typeIndex = data.typeIndex;
            }

            public bool Equals(MaterialOverrideData other) {
                return typeIndex == other.typeIndex;
            }
        }
    }
}

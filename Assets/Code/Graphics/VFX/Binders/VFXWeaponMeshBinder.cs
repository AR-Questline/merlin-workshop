using System;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.Kandra;
using Awaken.TG.Graphics.VFX.IndirectSamplingUniform;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/Weapon Mesh")]
    [VFXBinder("AR/Weapon Mesh")]
    public class VFXWeaponMeshBinder : VFXBinderBase {
        [SerializeField] Mode mode = Mode.DrakeMesh;

        [VFXPropertyBinding("UnityEngine.GraphicsBuffer"), SerializeField, FormerlySerializedAs("property")]
        ExposedProperty bakedMeshSampling = "BakedSampling";

        [SerializeField, VFXPropertyBinding("Awaken.Kandra.KandraVfxProperty")]
        ExposedProperty _kandraProperty = "KandraRenderer";
        ExposedProperty _vertexStart;
        ExposedProperty _additionalDataStart;
        ExposedProperty _vertexCount;
        ExposedProperty _trianglesCount;
        ExposedProperty _indicesBuffer;

        [VFXPropertyBinding("System.Single"), SerializeField]
        ExposedProperty maxSphereSize = "MaxSphereSize";

        [VFXPropertyBinding("System.UInt32"), SerializeField]
        ExposedProperty weaponSource = "LinkedWeapon";

        [SerializeField] int _samples = 256;

        Transform _weaponTransform;

        ARRuntimeUniformBaker _runtimeUniformBaker;

        KandraRenderer _kandraRenderer;
        KandraMesh _indicesMesh;
        GraphicsBuffer _indexBuffer;

        protected override void OnEnable() {
            base.OnEnable();
            UpdateSubProperties();
        }

        protected override void OnDisable() {
            if (_indexBuffer != null) {
#if UNITY_EDITOR
                if (KandraRendererManager.Instance != null) // As always Unity's editor is a mess :(
#endif
                {
                    KandraRendererManager.Instance.KandraVfxHelper.ReleaseIndexBuffer(_indicesMesh);
                }
            }
            _weaponTransform = null;
            _runtimeUniformBaker = null;
            _kandraRenderer = null;
            _indexBuffer = null;
            _indicesMesh = null;
            base.OnDisable();
        }

        void OnValidate() {
            UpdateSubProperties();
        }

        public override bool IsValid(VisualEffect component) {
            var isValid = mode;

#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                if (mode.HasFlagFast(Mode.DrakeMesh)) {
                    if (_runtimeUniformBaker == null) {
                        TrySetupDrake(component);

                        if (_weaponTransform) {
                            var effectTransform = component.transform;
                            effectTransform.parent = _weaponTransform;
                            effectTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                            effectTransform.localScale = Vector3.one;
                        }
                    }

                    if (_runtimeUniformBaker == null || _weaponTransform == null) {
                        isValid = isValid & ~Mode.DrakeMesh;
                    }
                }
                if (mode.HasFlagFast(Mode.KandraMesh)) {
                    if (_kandraRenderer == null) {
                        TrySetupKandra(component);
                    }

                    if (_kandraRenderer == null || KandraRendererManager.Instance.IsRegistered(_kandraRenderer.RenderingId) == false) {
                        isValid = isValid & ~Mode.KandraMesh;
                    }
                }
            }

            if (mode.HasFlagFast(Mode.DrakeMesh)) {
                isValid |= (IsValidDrake(component) ? Mode.DrakeMesh : 0);
            }
            if (mode.HasFlagFast(Mode.KandraMesh)) {
                isValid |= (IsValidKandra(component) ? Mode.KandraMesh : 0);
            }

            return isValid > 0 && (component.HasUInt(weaponSource) || math.countbits((int)mode) == 1);
        }

        bool IsValidDrake(VisualEffect component) {
            return component.HasGraphicsBuffer(bakedMeshSampling) &&
                   component.HasFloat(maxSphereSize);
        }

        bool IsValidKandra(VisualEffect component) {
            return component.HasUInt(_vertexStart) &&
                   component.HasUInt(_additionalDataStart) &&
                   component.HasUInt(_vertexCount) &&
                   component.HasUInt(_trianglesCount);
        }

        public override void UpdateBinding(VisualEffect component) {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                var weaponSourceValue = 0u;

                if (mode.HasFlagFast(Mode.DrakeMesh) && _runtimeUniformBaker?.IsBaked == true) {
                    _runtimeUniformBaker.Progress();
                    component.SetFloat(maxSphereSize, _runtimeUniformBaker.MeshBounds.size.magnitude);
                    component.SetGraphicsBuffer(bakedMeshSampling, _runtimeUniformBaker.SamplesBuffer);
                    weaponSourceValue = (uint)Mode.DrakeMesh;
                } else if (mode.HasFlagFast(Mode.KandraMesh) && _kandraRenderer && KandraRendererManager.Instance.TryGetInstanceData(_kandraRenderer, out var instanceData)) {
                    var vertexCount = _kandraRenderer.rendererData.mesh.vertexCount;
                    var indicesCounts = _kandraRenderer.rendererData.mesh.indicesCount;
                    var trianglesCount = indicesCounts / 3;

                    component.SetUInt(_vertexStart, instanceData.instanceStartVertex);
                    component.SetUInt(_additionalDataStart, instanceData.sharedStartVertex);
                    component.SetUInt(_vertexCount, vertexCount);
                    component.SetUInt(_trianglesCount, trianglesCount);
                    component.SetFloat(maxSphereSize, _kandraRenderer.rendererData.mesh.meshLocalBounds.size.magnitude);

                    if (component.HasGraphicsBuffer(_indicesBuffer)) {
                        if (_indexBuffer == null) {
                            _indicesMesh = _kandraRenderer.rendererData.mesh;
                            _indexBuffer = KandraRendererManager.Instance.KandraVfxHelper.GetIndexBuffer(_indicesMesh);
                        }
                        component.SetGraphicsBuffer(_indicesBuffer, _indexBuffer);
                    }

                    weaponSourceValue = (uint)Mode.KandraMesh;
                }

                if (math.countbits((int)mode) > 1) {
                    component.SetUInt(weaponSource, weaponSourceValue);
                }
            }
        }

        public override string ToString() {
            return Application.isPlaying ? "Weapon mesh" : "Weapon mesh (binding is only available in play mode)";
        }

        void UpdateSubProperties() {
            var mainProperty = _kandraProperty.ToString();
            _vertexStart = mainProperty + "_vertexStart";
            _additionalDataStart = mainProperty + "_additionalDataStart";
            _vertexCount = mainProperty + "_vertexCount";
            _trianglesCount = mainProperty + "_trianglesCount";
            _indicesBuffer = mainProperty + "_Indices";
        }

        void TrySetupDrake(VisualEffect component) {
            var linkedEntities = FindRenderingComponent<LinkedEntitiesAccess>(component);

            if (linkedEntities == null) {
                return;
            }

            var mesh = ExtractMesh(linkedEntities);
            if (!mesh) {
                return;
            }

            _runtimeUniformBaker = linkedEntities.GetComponent<ARRuntimeUniformBaker>();
            if (!_runtimeUniformBaker) {
                _runtimeUniformBaker = linkedEntities.gameObject.AddComponent<ARRuntimeUniformBaker>();
                _runtimeUniformBaker.sampleCount = _samples;
            }
            _runtimeUniformBaker.Bake(mesh);
            _weaponTransform = linkedEntities.transform;
        }

        void TrySetupKandra(VisualEffect component) {
            _kandraRenderer = FindRenderingComponent<KandraRenderer>(component);
        }
        
        T FindRenderingComponent<T>(VisualEffect vfxComponent) {
            var renderingOwner = vfxComponent as Component;

            if (vfxComponent.TryGetComponentInParent(out CharacterHandBase handBase)) {
                renderingOwner = handBase;
            } 
            else if (vfxComponent.TryGetComponentInParent(out InteractionObject interactionObject)) {
                renderingOwner = interactionObject;
            }
            
            return renderingOwner.GetComponentInChildren<T>();
        }

        static Mesh ExtractMesh(LinkedEntitiesAccess linkedEntities) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return null;
            }
            var entitiesManager = world.EntityManager;
            var meshEntity = Entity.Null;
            var maxLodMask = 0;

            foreach (var linkedEntity in linkedEntities.LinkedEntities) {
                if (entitiesManager.HasComponent<LODRange>(linkedEntity) && entitiesManager.HasComponent<MaterialMeshInfo>(linkedEntity)) {
                    var lodRange = entitiesManager.GetComponentData<LODRange>(linkedEntity);
                    var lodMask = lodRange.LODMask;
                    if (lodMask > maxLodMask) { // It's mask but for weapons we don't need to check bits
                        maxLodMask = lodMask;
                        meshEntity = linkedEntity;
                    }
                }
            }

            if (meshEntity == Entity.Null) {
                return null;
            }

            var materialMeshInfo = entitiesManager.GetComponentData<MaterialMeshInfo>(meshEntity);
            if (materialMeshInfo.IsRuntimeMesh) {
                var meshID = new BatchMeshID { value = (uint)materialMeshInfo.Mesh };
                return world.GetExistingSystemManaged<EntitiesGraphicsSystem>().GetMesh(meshID);
            }
            if (entitiesManager.HasComponent<RenderMesh>(meshEntity)) {
                var renderMesh = entitiesManager.GetSharedComponentManaged<RenderMeshArray>(meshEntity);
                return renderMesh.GetMesh(materialMeshInfo);
            }
            return null;
        }

        [Flags]
        enum Mode : byte {
            DrakeMesh = 1 << 0,
            KandraMesh = 1 << 1,
        }
    }
}
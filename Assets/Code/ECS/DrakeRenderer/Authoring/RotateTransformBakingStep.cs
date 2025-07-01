using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.ECS.Authoring;
using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer.Components;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class RotateTransformBakingStep : MonoBehaviour, IDrakeMeshRendererBakingStep,
        IDrakeMeshRendererBakingModificationStep, IDrakeLODBakingModificationStep {
        [SerializeField] bool _rotateAllMeshRenderers = true;

        [SerializeField, HideIf(nameof(_rotateAllMeshRenderers))]
        List<DrakeMeshRenderer> _drakeMeshRenderersToRotate;

        [SerializeField, Tooltip("Speed in degrees per second")]
        float _rotationSpeed = 360;

        [SerializeField] bool _useBaseRotationAxis = true;

        [SerializeField, ShowIf(nameof(_useBaseRotationAxis))]
        bool _useLocalSpaceBaseAxis = true;

        [SerializeField, ShowIf(nameof(_useBaseRotationAxis))]
        Axis _axis = Axis.Z;

        [SerializeField, HideIf(nameof(_useBaseRotationAxis)), Required]
        Transform _rotationAxisTransform;

        [SerializeField, HideIf(nameof(_useBaseRotationAxis))]
        bool _removeRotationAxisTransformOnBake;

        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity,
            in LodGroupSerializableData lodGroupData, in DrakeMeshMaterialComponent drakeMeshMaterialComponent,
            Entity entity, ref EntityCommandBuffer ecb) {
            if (!_rotateAllMeshRenderers && !_drakeMeshRenderersToRotate.Contains(drakeMeshRenderer)) {
                return;
            }

            float3 rotationAxis = GetRotationAxisInLocal();

            RemoveRotationAxisTransformIfNeeded();
            var rotateTransformComponent = new RotateTransformComponent(rotationAxis, math.radians(_rotationSpeed));
            ecb.AddComponent(entity, rotateTransformComponent);
        }

        public void ModifyDrakeLODGroup(DrakeLodGroup drakeLodGroup) {
            gameObject.isStatic = true;
            var options = new IWithUnityRepresentation.Options {
                movable = false
            };
            drakeLodGroup.SetUnityRepresentation(options);
        }

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            gameObject.isStatic = true;
            var options = new IWithUnityRepresentation.Options {
                movable = false
            };
            drakeMeshRenderer.SetUnityRepresentation(options);
            var maxExtents = math.cmax(drakeMeshRenderer.AABB.Extents);
            var newExtents = new float3(math.sqrt(2 * maxExtents * maxExtents));
            drakeMeshRenderer.EnsureBakingAABBExtents(newExtents);
        }

        float3 GetRotationAxisInLocal() {
            if (_useBaseRotationAxis) {
                var baseAxis = GetBaseAxis(_axis);
                if (_useLocalSpaceBaseAxis == false) {
                    return transform.InverseTransformDirection(baseAxis).normalized;
                }
                return baseAxis;
            }

            if (_rotationAxisTransform != null) {
                return transform.InverseTransformDirection(_rotationAxisTransform.transform.forward).normalized;
            }
            return float3.zero;
        }

        void RemoveRotationAxisTransformIfNeeded() {
            if (!_useBaseRotationAxis && _rotationAxisTransform != null && _removeRotationAxisTransformOnBake &&
                Application.isPlaying
#if UNITY_EDITOR
                && !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(_rotationAxisTransform.gameObject)
#endif
               ) {
                Destroy(_rotationAxisTransform.gameObject);
            }
        }

        static float3 GetBaseAxis(Axis axis) {
            return axis switch {
                Axis.X => new float3(1, 0, 0),
                Axis.Y => new float3(0, 1, 0),
                Axis.Z => new float3(0, 0, 1),
                _ => float3.zero
            };
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            Gizmos.color = Color.yellow;
            var startPos = transform.position;
            var endPos = startPos + transform.TransformDirection(GetRotationAxisInLocal());
            Gizmos.DrawLine(startPos, endPos);
            Gizmos.color = Color.white;
        }
#endif

        enum Axis : byte {
            X = 0,
            Y = 1,
            Z = 2
        }
    }
}
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Graphics.Lines {
    [BurstCompile]
    public class BezierCurveLine : MonoBehaviour, UnityUpdateProvider.IWithLateUpdateGeneric {
        [SerializeField, Required] LineRenderer lineRenderer;
        [SerializeField, Required] Transform startPointTransform;
        [SerializeField, Required] Transform endPointTransform;
        [SerializeField, Min(3)] int samplesCount = 15;
        [SerializeField] Axis xAxisMapping = Axis.X;
        [SerializeField] float curveScalingFactor = 4;
        [SerializeField] float lineTextureScaleX = 1;
        [SerializeField] bool revertLineDirection;
        [SerializeField] bool syncObjectWithEndPoint;

        [SerializeField, Required, ShowIf(nameof(syncObjectWithEndPoint))]
        Transform synchronizedWithEndPointTransform;

        [SerializeField] bool checkCollision;

        [SerializeField, ShowIf(nameof(checkCollision))]
        float collisionSphereRadius = 1;

        [SerializeField, ShowIf(nameof(checkCollision))]
        LayerMask collisionLayers = new() { value = RenderLayers.Mask.Default };

        NativeArray<Vector3> _curvePositions;

        void OnEnable() {
            _curvePositions = new NativeArray<Vector3>(samplesCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
            UnityUpdateProvider.GetOrCreate().RegisterLateGeneric(this);
        }

        void OnDisable() {
            UnityUpdateProvider.GetOrCreate().UnregisterLateGeneric(this);
            _curvePositions.Dispose();
        }

        public void UnityLateUpdate(float deltaTime) {
            UpdateLineRendererPositions();
        }

        void UpdateLineRendererPositions() {
            if (startPointTransform == null || endPointTransform == null || lineRenderer == null) {
#if UNITY_EDITOR
                if (Application.isPlaying)
#endif
                {
                    Log.Important?.Error($"Some of required references in {nameof(BezierCurveLine)} {name} are not set", this);
                    if (lineRenderer != null) {
                        lineRenderer.enabled = false;
                    }

                    enabled = false;
                }

                return;
            }

            float curveScale = 1;
            startPointTransform.GetPositionAndRotation(out var startPointPos, out var startPointRot);
            endPointTransform.GetPositionAndRotation(out var endPointPos, out var endPointRot);
            if (checkCollision) {
                CollisionCheck(startPointPos, collisionSphereRadius, collisionLayers.value, ref endPointPos, ref curveScale);
            }
#if UNITY_EDITOR
            if (_curvePositions.IsCreated == false || _curvePositions.Length != samplesCount) {
                _curvePositions.Dispose();
                _curvePositions = new NativeArray<Vector3>(samplesCount, ARAlloc.Persistent, NativeArrayOptions.UninitializedMemory);
            }
#endif

            var xAxisVector = GetAxisVector(xAxisMapping);
            var startPointRightDir = startPointRot * xAxisVector;
            var endPointRightDir = endPointRot * xAxisVector;
            var startPointScale = startPointTransform.localScale.x * curveScalingFactor;
            var endPointScale = endPointTransform.localScale.x * curveScalingFactor;

            SetCurvePositions(
                in startPointPos, in startPointRightDir, in startPointScale,
                in endPointPos, in endPointRightDir, in endPointScale, in samplesCount, in curveScale, in revertLineDirection,
                ref _curvePositions);

            float curveLength = 0;
            for (int i = 0; i < _curvePositions.Length - 1; i++) {
                curveLength += math.distance(_curvePositions[i], _curvePositions[i + 1]);
            }

            lineRenderer.positionCount = _curvePositions.Length;
            lineRenderer.SetPositions(_curvePositions);
            var textureScale = lineRenderer.textureScale;
            textureScale.x = curveLength / this.lineTextureScaleX;
            
            lineRenderer.textureScale = textureScale;
            if (syncObjectWithEndPoint && synchronizedWithEndPointTransform != null) {
                synchronizedWithEndPointTransform.SetPositionAndRotation(endPointPos, endPointRot);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (Application.isPlaying == false) {
                UpdateLineRendererPositions();
            }

            if (_curvePositions.IsCreated) {
                for (int i = 0; i < _curvePositions.Length - 1; i++) {
                    Debug.DrawLine(_curvePositions[i], _curvePositions[i + 1], Color.green);
                }
            }

            if (endPointTransform != null && checkCollision) {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(endPointTransform.position, collisionSphereRadius);
            }
        }
#endif
        static void CollisionCheck(Vector3 startPoint, float collisionSphereRadius, int collisionMask, ref Vector3 endPoint, ref float curveScale) {
            var lineVector = endPoint - startPoint;
            var lineLength = math.length(lineVector);
            if (lineLength < 0.01f) {
                return;
            }

            var lineDirection = lineVector / lineLength;
            if (Physics.SphereCast(startPoint, collisionSphereRadius, lineDirection, out var hit, lineLength, collisionMask)) {
                var sphereCenterAfterHit = hit.point + (hit.normal * collisionSphereRadius);
                var newLineLength = math.max(math.length(sphereCenterAfterHit - startPoint), 0.01f);
                curveScale = newLineLength / lineLength;
                endPoint = startPoint + (lineDirection * newLineLength);
            }
        }

        static Vector3 GetAxisVector(Axis axis) {
            var axisVector = new float3(0);
            if ((int)axis < 3) {
                axisVector[(int)axis] = 1;
            } else {
                axisVector[((int)axis) - 3] = -1;
            }

            return axisVector;
        }

        [BurstCompile]
        static void SetCurvePositions(
            in Vector3 startPointPos, in Vector3 startPointRightDir, in float startPointScale,
            in Vector3 endPointPos, in Vector3 endPointRightDir, in float endPointScale,
            in int samplesCount, in float curveScale, in bool revertLineDirection,
            ref NativeArray<Vector3> curvePositions) {
            var startPoint = startPointPos;
            var endPoint = endPointPos;

            var stepSize = 1f / (samplesCount - 1);
            var startPointHandlePos = startPoint + startPointRightDir * startPointScale * curveScale;
            var endPointHandlePos = endPoint + endPointRightDir * endPointScale * -curveScale;

            if (revertLineDirection) {
                for (int i = 0; i < samplesCount; i++) {
                    var t = 1 - stepSize * i;
                    curvePositions[i] = CubicBezierCurve(startPoint, startPointHandlePos, endPointHandlePos, endPoint, t);
                }
            } else {
                for (int i = 0; i < samplesCount; i++) {
                    var t = stepSize * i;
                    curvePositions[i] = CubicBezierCurve(startPoint, startPointHandlePos, endPointHandlePos, endPoint, t);
                }
            }
        }

        static float3 CubicBezierCurve(float3 p1, float3 p2, float3 p3, float3 p4, float t) {
            var a = math.lerp(p1, p2, t);
            var b = math.lerp(p2, p3, t);
            var c = math.lerp(p3, p4, t);

            var d = math.lerp(a, b, t);
            var e = math.lerp(b, c, t);

            return math.lerp(d, e, t);
        }

        [UnityEngine.Scripting.Preserve]
        enum Axis : byte {
            // Should be in this exact order
            [UnityEngine.Scripting.Preserve] X = 0,
            [UnityEngine.Scripting.Preserve] Y = 1,
            [UnityEngine.Scripting.Preserve] Z = 2,

            [UnityEngine.Scripting.Preserve] [InspectorName("-X")]
            MinusX = 3,

            [UnityEngine.Scripting.Preserve] [InspectorName("-Y")]
            MinusY = 4,

            [UnityEngine.Scripting.Preserve] [InspectorName("-Z")]
            MinusZ = 5
        }
    }
}
using System.Linq;
using Awaken.Utility.Maths.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Utility.Maths {
    public class BezierCurvePresenter : MonoBehaviour {
        public Transform startPoint;
        public Transform endPoint;
        public Transform controlPoint1;
        public Transform controlPoint2;
        [Range(0.0001f, 0.1f)]public float steps = 0.01f;
        [Range(0f, 1f)]public float currentStep = 0.5f;
        public Mode mode = Mode.Time;

        public enum Mode {
            Time,
            NormalizedDistance,
            Distance,
            AllPoints
        }
#if UNITY_EDITOR
        void OnDrawGizmos() {
            EnsurePoints();
            DrawBezier();
        }

        void DrawBezier() {
            var bezier = new BezierCurve(startPoint.position, controlPoint1.position, controlPoint2.position, endPoint.position, steps);

            DrawMainPoints();
            foreach (var bezierSample in bezier.Samples) {
                Gizmos.color = Color.grey;
                Gizmos.DrawSphere(bezierSample.Position, Mathf.Clamp(steps, 0.005f, 0.05f) * 10f);
            }

            DrawCurrentPoint(bezier);
        }

        void DrawCurrentPoint(BezierCurve bezier) {
            BezierCurve.Point currentPoint;
            if (mode == Mode.Time) {
                currentPoint = bezier.ExactlyAt(currentStep);
            }else if (mode == Mode.NormalizedDistance) {
                currentPoint = bezier.AtByDistance(currentStep);
            }else if (mode == Mode.Distance) {
                float t = bezier.Distance * currentStep;
                currentPoint = bezier.AtDistance(t);
            } else {
                int sample = Mathf.RoundToInt(currentStep * bezier.Samples.Count);
                sample = Mathf.Clamp(sample, 0, bezier.Samples.Count - 1);
                currentPoint = bezier.Samples.ElementAt(sample);
            }
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentPoint.Position, 0.5f);
            Gizmos.DrawSphere(currentPoint.Position, 0.01f);
            #if UNITY_EDITOR
            Handles.Label(currentPoint.Position + currentPoint.Tangent.normalized, currentPoint.Distance.ToString());
            #endif
            Gizmos.DrawLine(currentPoint.Position + currentPoint.Tangent.normalized, currentPoint.Position - currentPoint.Tangent.normalized);
        }

        void DrawMainPoints() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(startPoint.position, 0.2f);
            Gizmos.DrawSphere(endPoint.position, 0.2f);
            
            Gizmos.color = Color.grey;
            Gizmos.DrawSphere(controlPoint1.position, 0.2f);
            Gizmos.DrawSphere(controlPoint2.position, 0.2f);
            
            Gizmos.DrawLine(startPoint.position, controlPoint1.position);
            Gizmos.DrawLine(endPoint.position, controlPoint2.position);
        }

        void EnsurePoints() {
            CreatePointIfNull(ref startPoint, nameof(startPoint));
            CreatePointIfNull(ref endPoint, nameof(endPoint));
            CreatePointIfNull(ref controlPoint1, nameof(controlPoint1));
            CreatePointIfNull(ref controlPoint2, nameof(controlPoint2));
        }

        void CreatePointIfNull(ref Transform assignment, string name) {
            if (assignment == null) {
                CreatePoint(ref assignment, name);
            }
        }
        
        void CreatePoint(ref Transform assignment, string name) {
            var newGo = new GameObject(name);
            var newGoTransform = newGo.transform;
            newGoTransform.SetParent(transform);
            newGoTransform.localPosition = Vector3.zero;
            newGoTransform.localRotation = Quaternion.identity;
            newGoTransform.localScale = Vector3.one;
            assignment = newGoTransform;
        }
#endif
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    public class BezierCurve {
        public Vector3 StartPoint { get; }
        public Vector3 EndPoint { get; }
        public Vector3 ControlPoint1 { get; }
        public Vector3 ControlPoint2 { get; }
        public float Distance { get; private set; }
        public IReadOnlyCollection<Point> Samples => _samples;
        
        List<Point> _samples = new List<Point>();

        public BezierCurve(Vector3 startPoint, Vector3 controlPoint1, Vector3 controlPoint2, Vector3 endPoint, float step = 0.01f) {
            StartPoint = startPoint;
            EndPoint = endPoint;
            ControlPoint1 = controlPoint1;
            ControlPoint2 = controlPoint2;
            
            float progress = 0;
            AddPointAt(progress);
            
            while (progress < 1f) {
                progress += step;
                AddPointAt(progress);
            }
        }

        void AddPointAt(float t) {
            var previous = _samples.LastOrDefault();
            var currentPoint = BezierCurveUtil.Sample(StartPoint, ControlPoint1, ControlPoint2, EndPoint, t);

            if (previous != null) {
                Distance += Vector3.Distance(previous.Position, currentPoint);
            }
            
            
            var newPoint = new Point(
                currentPoint,
                BezierCurveUtil.SampleTangent(StartPoint, ControlPoint1, ControlPoint2, EndPoint, t),
                Distance
            );
            
            _samples.Add(newPoint);
        }

        /// <summary>
        /// Point at curve at given time (normalized delta progression)
        /// </summary>
        /// <remarks>
        /// The point is exactly at given time but without distance
        /// </remarks>
        /// <param name="t">Time (normalized delta progression)</param>
        public Point ExactlyAt(float t) {
            return new Point(
                BezierCurveUtil.Sample(StartPoint, ControlPoint1, ControlPoint2, EndPoint, t),
                BezierCurveUtil.SampleTangent(StartPoint, ControlPoint1, ControlPoint2, EndPoint, t),
                0
            );
        }

        /// <summary>
        /// Point at approximately given time normalized by distance
        /// </summary>
        /// <param name="t">Time in normalized distance</param>
        /// <returns></returns>
        public Point AtByDistance(float t) {
            t = Mathf.Clamp01(t);

            if (Mathf.Approximately(t, 0)) {
                return _samples[0];
            }
            if (Mathf.Approximately(t, 1)) {
                return _samples.Last();
            }

            Point overPoint = null;
            int i = 0;
            while (overPoint == null) {
                i++;
                if (_samples[i].Distance / Distance >= t) {
                    overPoint = _samples[i];
                }
            }

            Point previousPoint = _samples[i - 1];

            float lerpT = (t - previousPoint.Distance / Distance) / (overPoint.Distance / Distance - previousPoint.Distance/ Distance);
            
            return new Point( 
                Vector3.Lerp(previousPoint.Position, overPoint.Position, lerpT),
                Vector3.Lerp(previousPoint.Tangent, overPoint.Tangent, lerpT),
                Mathf.Lerp(previousPoint.Distance, overPoint.Distance, lerpT)
                );
        }

        public Point AtDistance(float distance) {
            distance = Mathf.Clamp(distance, 0, Distance);
            
            if (Mathf.Approximately(distance, 0)) {
                return _samples[0];
            }

            if (Mathf.Approximately(distance, Distance)) {
                return _samples.Last();
            }
            
            Point overPoint = null;
            int i = 0;
            while (overPoint == null) {
                i++;
                if (_samples[i].Distance >= distance) {
                    overPoint = _samples[i];
                }
            }

            Point previousPoint = _samples[i - 1];
            
            float lerpT = (distance - previousPoint.Distance) / (overPoint.Distance - previousPoint.Distance);
            return new Point( 
                Vector3.Lerp(previousPoint.Position, overPoint.Position, lerpT),
                Vector3.Lerp(previousPoint.Tangent, overPoint.Tangent, lerpT),
                Mathf.Lerp(previousPoint.Distance, overPoint.Distance, lerpT)
            );
        }

        public bool NeedUpdate(Vector3 startPoint, Vector3 controlPoint1, Vector3 controlPoint2, Vector3 endPoint, float step = 0.01f) {
            var newCount = Mathf.RoundToInt(1f / step);
            if (Mathf.Abs(_samples.Count - newCount) > Mathf.RoundToInt(_samples.Count * 0.005f)) {
                return true;
            }

            var distanceThreshold = Distance * Distance * 0.005f;
            if (Vector3.SqrMagnitude(StartPoint - startPoint) > distanceThreshold) {
                return true;
            }
            if (Vector3.SqrMagnitude(EndPoint - endPoint) > distanceThreshold) {
                return true;
            }
            if (Vector3.SqrMagnitude(ControlPoint1 - controlPoint1) > distanceThreshold) {
                return true;
            }
            if (Vector3.SqrMagnitude(ControlPoint2 - controlPoint2) > distanceThreshold) {
                return true;
            }

            return false;
        }

        public class Point {
            public Vector3 Position { get; }
            public Vector3 Tangent { get; }
            public float Distance { get; }

            public Point(Vector3 position, Vector3 tangent, float distance) {
                Position = position;
                Tangent = tangent;
                Distance = distance;
            }
        }
    }

    public static class BezierCurveUtil {
        public static Vector3 Sample(Vector3 startPoint, Vector3 controlPoint1, Vector3 controlPoint2, Vector3 endPoint, float t) {
            t = Mathf.Clamp01(t);
            float t2 = 1-t;
            return t2*t2*t2 * startPoint + 3 * t2*t2 * t * controlPoint1 + 3 * t2 * t*t * controlPoint2 + t*t*t * endPoint;
        }
        
        public static Vector3 SampleTangent (Vector3 startPoint, Vector3 controlPoint1, Vector3 controlPoint2, Vector3 endPoint, float t) {
            t = Mathf.Clamp01(t);
            float t2 = 1-t;
            return 3*t2*t2*(controlPoint1-startPoint) + 6*t2*t*(controlPoint2 - controlPoint1) + 3*t*t*(endPoint - controlPoint2);
        }
    }
}
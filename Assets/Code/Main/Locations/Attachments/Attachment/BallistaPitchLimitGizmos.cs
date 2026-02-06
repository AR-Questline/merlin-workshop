using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    public static class BallistaPitchLimitGizmos {
        const int Segments = 20;
        const float BaseArcRadius = 5f;

        public static void DrawPitchLimits(Vector3 position, Transform ballistaBase, float pitchLimit) {
            var arcRadius = GetScaledRadius(ballistaBase);
            
            // Draw upper pitch limit arc (positive pitch)
            Gizmos.color = Color.cyan;
            DrawPitchArc(position, ballistaBase, pitchLimit, arcRadius, Segments);
            
            // Draw lower pitch limit arc (negative pitch)
            Gizmos.color = Color.magenta;
            DrawPitchArc(position, ballistaBase, -pitchLimit, arcRadius, Segments);
            
            // Draw horizontal reference plane in ballista's local space
            // This is the ballista's local forward direction (0 pitch in its own coordinate system)
            Gizmos.color = Color.green;
            var localForward = ballistaBase.rotation * Vector3.forward;
            Gizmos.DrawLine(position, position + localForward * arcRadius);
        }

        public static void DrawYawLimits(Vector3 position, Transform ballistaBase, float yawLimit) {
            var arcRadius = GetScaledRadius(ballistaBase);
            
            // Draw left yaw limit arc (negative yaw)
            Gizmos.color = Color.yellow;
            DrawYawArc(position, ballistaBase, -yawLimit, arcRadius, Segments);
            
            // Draw right yaw limit arc (positive yaw)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            DrawYawArc(position, ballistaBase, yawLimit, arcRadius, Segments);
        }

        static float GetScaledRadius(Transform ballistaBase) {
            // Use the maximum scale component to ensure arcs are outside the model
            var scale = ballistaBase.lossyScale;
            var maxScale = Mathf.Max(scale.x, scale.y, scale.z);
            return BaseArcRadius * maxScale;
        }

        static void DrawPitchArc(Vector3 position, Transform ballistaBase, float pitchAngle, float radius, int segments) {
            // Build rotation in local space, matching the runtime logic
            // Local horizontal direction is just forward in local XZ plane
            var localYawRotation = Quaternion.identity; // In local space, we're already aligned
            var localPitchRotation = Quaternion.AngleAxis(-pitchAngle, Vector3.right);
            var localRotation = localYawRotation * localPitchRotation;
            
            // Transform to world space
            var worldRotation = ballistaBase.rotation * localRotation;
            var direction = worldRotation * Vector3.forward;
            
            // Draw line showing the pitch limit direction
            Gizmos.DrawLine(position, position + direction * radius);
            
            // Draw arc from horizontal to pitch limit
            var startAngle = 0f;
            var endAngle = pitchAngle;
            var angleStep = (endAngle - startAngle) / segments;
            
            // Start from ballista's local forward direction (0 pitch in its own coordinate system)
            var localForward = ballistaBase.rotation * Vector3.forward;
            var previousPoint = position + localForward * radius;
            
            for (int i = 1; i <= segments; i++) {
                var angle = startAngle + angleStep * i;
                var localRot = Quaternion.AngleAxis(-angle, Vector3.right);
                var worldRot = ballistaBase.rotation * localRot;
                var point = position + worldRot * Vector3.forward * radius;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }

        static void DrawYawArc(Vector3 position, Transform ballistaBase, float yawAngle, float radius, int segments) {
            // Build rotation in local space for yaw
            var localYawRotation = Quaternion.AngleAxis(yawAngle, Vector3.up);
            
            // Transform to world space
            var worldRotation = ballistaBase.rotation * localYawRotation;
            var direction = worldRotation * Vector3.forward;
            
            // Draw line showing the yaw limit direction
            Gizmos.DrawLine(position, position + direction * radius);
            
            // Draw arc from center to yaw limit
            var startAngle = 0f;
            var endAngle = yawAngle;
            var angleStep = (endAngle - startAngle) / segments;
            
            // Start from ballista's local forward direction (0 yaw in its own coordinate system)
            var localForward = ballistaBase.rotation * Vector3.forward;
            var previousPoint = position + localForward * radius;
            
            for (int i = 1; i <= segments; i++) {
                var angle = startAngle + angleStep * i;
                var localRot = Quaternion.AngleAxis(angle, Vector3.up);
                var worldRot = ballistaBase.rotation * localRot;
                var point = position + worldRot * Vector3.forward * radius;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
    }
}


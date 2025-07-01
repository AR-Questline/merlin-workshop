using UnityEngine;
using UnityEngine.Animations;

namespace Awaken.TG.Graphics.VFX {
    /// <summary>
    /// This is marker MonoBehaviour for VFX that should be attached to the hit collider.
    /// </summary>
    public class AttachMeToHitCollider : MonoBehaviour {
        public void AttachToTransform(Transform parent) {
            if (!gameObject.activeInHierarchy) {
                return;
            }
            
            PositionConstraint positionConstraint = GetComponent<PositionConstraint>();
            if (positionConstraint == null) {
                positionConstraint = gameObject.AddComponent<PositionConstraint>();
            } else {
                ClearConstraintSources(positionConstraint);
            }
            positionConstraint.AddSource(new ConstraintSource { sourceTransform = parent, weight = 1 });
            positionConstraint.constraintActive = true;

            RotationConstraint rotationConstraint = GetComponent<RotationConstraint>();
            if (rotationConstraint == null) {
                rotationConstraint = gameObject.AddComponent<RotationConstraint>();
            } else {
                ClearConstraintSources(rotationConstraint);
            }
            rotationConstraint.AddSource(new ConstraintSource { sourceTransform = parent, weight = 1 });
            rotationConstraint.constraintActive = true;
        }

        void OnDisable() {
            PositionConstraint positionConstraint = GetComponent<PositionConstraint>();
            if (positionConstraint != null) {
                ClearConstraintSources(positionConstraint);
            }
            
            RotationConstraint rotationConstraint = GetComponent<RotationConstraint>();
            if (rotationConstraint != null) {
                ClearConstraintSources(rotationConstraint);
            }
        }

        static void ClearConstraintSources(IConstraint positionConstraint) {
            for (int i = 0; i < positionConstraint.sourceCount; i++) {
                positionConstraint.RemoveSource(i);
            }
        }
    }
}
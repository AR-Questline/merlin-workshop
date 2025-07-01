using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG {
    public class RescaleChildrenOdin : MonoBehaviour {
        [Button("Rescale Children")]
        public void Rescale() {
#if UNITY_EDITOR
            Undo.RecordObject(transform, "Rescale Parent");
            foreach (Transform child in transform) {
                Undo.RecordObject(child, "Rescale Child");
            }
#endif
            Vector3 parentScale = transform.localScale;
            
            foreach (Transform child in transform) {
                Vector3 globalPosition = transform.TransformPoint(child.localPosition);
                Quaternion globalRotation = transform.rotation * child.localRotation;
                
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                
                child.localPosition = transform.InverseTransformPoint(globalPosition);
                child.localRotation = Quaternion.Inverse(transform.rotation) * globalRotation;
                
                child.localScale = Vector3.Scale(child.localScale, parentScale);
            }
            
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

#if UNITY_EDITOR
            EditorUtility.SetDirty(transform);
#endif
        }
    }
}
using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using UnityEngine;

namespace Awaken.Utility {
    public class DirectionGizmos : MonoBehaviour, IEditorOnlyMonoBehaviour {
#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            transform.GetPositionAndRotation(out var position, out var rotation);

            var pos = position + rotation * Vector3.up * 0.1f;
            var origin1 = pos + rotation * Vector3.right * 0.15f;
            var origin2 = pos - rotation * Vector3.right * 0.15f;
            var origin3 = pos + rotation * Vector3.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin1, origin3);
            Gizmos.DrawLine(origin2, origin3);
            Gizmos.DrawLine(origin1, origin2);
            Gizmos.DrawSphere(pos, 0.15f);
        }
#endif
    }
}

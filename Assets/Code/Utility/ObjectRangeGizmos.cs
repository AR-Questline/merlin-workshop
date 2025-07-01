using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using UnityEngine;

namespace Awaken.Utility {
    public class ObjectRangeGizmos : MonoBehaviour, IEditorOnlyMonoBehaviour {
#if UNITY_EDITOR
        [SerializeField] bool wireSphere = true;
        [SerializeField] bool showOnlyWhenSelected;
        [SerializeField] float gizmoSize = 3;
        [SerializeField] Color gizmosColor = Color.green;

        void OnDrawGizmos() {
            if (!showOnlyWhenSelected) {
                Draw();
            }
        }

        void OnDrawGizmosSelected() {
            if (showOnlyWhenSelected) {
                Draw();
            }
        }

        void Draw() {
            Gizmos.color = gizmosColor;
            if (wireSphere) {
                Gizmos.DrawWireSphere(this.transform.position, gizmoSize);
            } else {
                Gizmos.DrawSphere(this.transform.position, gizmoSize);
            }
        }
#endif
    }
}


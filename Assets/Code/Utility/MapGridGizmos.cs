using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility {
    [TypeInfoBox("This object will be root after unfold." +
                 " Hierarchy unfolding can destroy this GameObject. See SceneUnfold.cs for more info.")]
    public class MapGridGizmos : MonoBehaviour, IFutureRootAfterUnfoldMarker, IEditorOnlyTransform {
        public Vector3 gizmoSize = new Vector3(512, 512, 512);
        public Color gizmosColor = Color.green;

        public GameObject GameObject => gameObject;
        bool IEditorOnlyTransform.PreserveChildren => true;

#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            Gizmos.color = gizmosColor;
            Gizmos.DrawWireCube(this.transform.position, gizmoSize);
        }
#endif
    }
}

using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    /// <summary>
    /// Unity for some reason makes copy GameObject outside of scene (null scene), so to remove them we need this script
    /// </summary>
    [ExecuteAlways]
    public class HackBugRemoval : MonoBehaviour {
        void Start() {
            if (GetComponentInParent<DrakeMeshRenderer>() == null && GetComponentInParent<DrakeLodGroup>() == null) {
                DestroyImmediate(gameObject);
            }
        }
    }
}

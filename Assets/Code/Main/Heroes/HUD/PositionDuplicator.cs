using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    /// <summary>
    /// Used to avoid UI conflicts. For example: Image A wants to be higher in vertical layout than image B, but B should be rendered first.
    /// </summary>
    [ExecuteInEditMode]
    public class PositionDuplicator : MonoBehaviour {
        public Transform source;

        void OnGUI() {
            if (source != null) {
                transform.position = source.position;
            }
        }
    }
}
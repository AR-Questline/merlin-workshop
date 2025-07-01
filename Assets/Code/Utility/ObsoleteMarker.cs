using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.Utility {
    [ExecuteAlways]
    public class ObsoleteMarker : MonoBehaviour, IEditorOnlyMonoBehaviour {
#if UNITY_EDITOR
        [SerializeField] Object useInstead;
        [SerializeField, TextArea(3, 8)] string reason;

        void OnEnable() {
            Log.Critical?.Error($"Obsolete <color=yellow>{gameObject.name}</color>! Use <color=yellow>{useInstead?.name}</color> instead.\nReason: {reason}", gameObject, LogOption.NoStacktrace);
        }
#endif
    }
}
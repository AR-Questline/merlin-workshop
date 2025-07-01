using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    [System.Serializable]
    public class VirtualCameraReferences {
        [UnityEngine.Scripting.Preserve] public Transform follow;
        [UnityEngine.Scripting.Preserve] public Transform lookAt;
    }
}
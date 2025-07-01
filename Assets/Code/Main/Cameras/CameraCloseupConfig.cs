using UnityEngine;

namespace Awaken.TG.Main.Cameras {
    /// <summary>
    /// Config of Camera Closeup behaviour for specific place.
    /// </summary>
    [System.Serializable]
    public class CameraCloseupConfig {

        // === Fields
        
        /// All vectors are in local space.
        [UnityEngine.Scripting.Preserve] public Vector3 closeupStartPos;
        [UnityEngine.Scripting.Preserve] public Vector3 closeupEndPos;
        [UnityEngine.Scripting.Preserve] public Quaternion closeupRotation;

        /// <summary>
        /// Default CloseupConfig for locations.
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static readonly CameraCloseupConfig Default = new CameraCloseupConfig() {
            closeupStartPos = new Vector3(0f, 1.5f, -3.8f),
            closeupEndPos = new Vector3(1.39f, 0.817f, -2.51f),
            closeupRotation = Quaternion.Euler(27f, -9.4f, 0f),
        };
    }
}
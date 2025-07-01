using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Cameras {
    /// <summary>
    /// Interface for models that can be targeted by Closeup Camera.
    /// </summary>
    public interface ICloseup : IModel {
        /// <summary>
        /// Configuration of camera movement for this closeup object.
        /// All vectors are in local space.
        /// </summary>
        [UnityEngine.Scripting.Preserve] CameraCloseupConfig CloseupConfig { get; }

        /// <summary>
        /// Matrix data of closeup object. Used to transform local-space position/scale to world space.
        /// </summary>
        [UnityEngine.Scripting.Preserve] Matrix4x4 PrefabToWorldMatrix { get; }

        /// <summary>
        /// Allows objects to deny closeup in special conditions.
        /// </summary>
        [UnityEngine.Scripting.Preserve] bool AllowCloseup { get; }
    }
}
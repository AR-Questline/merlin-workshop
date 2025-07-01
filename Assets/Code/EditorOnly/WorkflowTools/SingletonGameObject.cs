#if UNITY_EDITOR
using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using UnityEngine;

namespace Awaken.TG.EditorOnly.WorkflowTools { 
    /// <summary>
    /// Marker component for SingletonGameObject system. Provides apply all menu item in "TG/Scene Tools/" and applies top-level changes on saving prefabs/scene
    /// </summary>
    [DisallowMultipleComponent]
    public class SingletonGameObject : MonoBehaviour, IEditorOnlyMonoBehaviour { }
}
#endif
using UnityEngine;

namespace Awaken.Utility.Scenes {
    /// <summary>
    /// Executes on entering PlayMode and on Build after StaticSceneSubdivision
    /// </summary>
    public abstract class SceneProcessorComponent : MonoBehaviour {
        public abstract void Process();
    }
    
    /// <summary>
    /// Executes on entering PlayMode and on Build before StaticSceneSubdivision
    /// </summary>
    public abstract class ScenePreProcessorComponent : SceneProcessorComponent { }
}
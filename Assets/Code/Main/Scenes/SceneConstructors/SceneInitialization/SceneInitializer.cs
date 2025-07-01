using System.Linq;
using Awaken.TG.Debugging;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization {
    /// <summary>
    /// Service that provides access to current scene initialization process
    /// <see cref="SceneInitializationHandle"/>
    /// </summary>
    public class SceneInitializer : IService {
        public SceneInitializationHandle SceneInitializationHandle { get; } = new SceneInitializationHandle();

        // Shortcuts
        public SceneInitializationHandle.ElementHandle GetNewElement(string name) {
            Log.Marking?.Warning($"Initialization step [{name}]: Started");
            return SceneInitializationHandle.GetNewElement(name);
        }
        
        public void CompleteElement(string name) {
            SceneInitializationHandle.ElementHandle remainingElement = SceneInitializationHandle.RemainedElements.FirstOrDefault(e => e.Name == name);
            if (remainingElement != null) {
                remainingElement.Complete();
                Log.Marking?.Warning($"Initialization step [{name}]: Completed");
            }
        }

        public void Clear() {
            SceneInitializationHandle.Clear();
        }
    }
}
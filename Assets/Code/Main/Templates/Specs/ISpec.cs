using Awaken.TG.MVC;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Awaken.TG.Main.Templates.Specs {
    /// <summary>
    /// Interface for specifications that can be edited in scene view and are later
    /// spawned as models at runtime according to that specification.
    /// </summary>
    public interface ISpec {
        /// <summary>
        /// Prepares a model based on the data from this spec.
        /// </summary>
        Model CreateModel();
        
        /// <summary>
        /// Usually specs are spawned only on scene initialization, not restoration. This bool makes it spawn in both scenarios.
        /// </summary>
        bool SpawnOnRestore { get; }

        // specs are mono behaviors, so this will be available automatically
        // through inheritance
        [UnityEngine.Scripting.Preserve] GameObject gameObject { get; }
        [UnityEngine.Scripting.Preserve] Transform transform { get; }
    }
}
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Assets {
    public class SceneEqualityComparer : IEqualityComparer<Scene> {
        public bool Equals(Scene x, Scene y) {
            return x.handle == y.handle;
        }

        public int GetHashCode(Scene obj) {
            return obj.handle;
        }
    }
}
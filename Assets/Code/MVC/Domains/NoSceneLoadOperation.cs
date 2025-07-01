using System.Collections.Generic;
using System.Linq;

namespace Awaken.TG.MVC.Domains {
    public class NoSceneLoadOperation : ISceneLoadOperation {
        public string Name => "No Load Operation";
        public bool IsDone => true;
        public float Progress => 1f;
        public IEnumerable<string> MainScenesNames => Enumerable.Empty<string>();
    }
}
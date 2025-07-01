using System.Collections.Generic;

namespace Awaken.TG.MVC.Domains {
    public interface ISceneLoadOperation {
        string Name { get; }
        bool IsDone { get; }
        float Progress { get; }
        IEnumerable<string> MainScenesNames { get; }
    }
}
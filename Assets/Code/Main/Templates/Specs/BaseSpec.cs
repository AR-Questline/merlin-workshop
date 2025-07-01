using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Templates.Specs {
    public abstract class BaseSpec : SceneSpec, ISpec {
        public abstract Model CreateModel();
        public virtual bool SpawnOnRestore => false;
    }
}
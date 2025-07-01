using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    public interface IScene {
        ISceneLoadOperation Unload(bool isSameSceneReloading);
    }
}
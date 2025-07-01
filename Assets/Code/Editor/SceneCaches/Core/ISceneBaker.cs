using Awaken.TG.Assets;

namespace Awaken.TG.Editor.SceneCaches.Core {
    public interface ISceneBaker {
        void StartBaking();
        void Bake(SceneReference scene);
        void FinishBaking();
    }
}
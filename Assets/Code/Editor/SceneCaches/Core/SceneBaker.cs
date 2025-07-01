using Awaken.TG.Assets;
using Awaken.TG.Main.General.Caches;

namespace Awaken.TG.Editor.SceneCaches.Core {
    public abstract class SceneBaker<T> : ISceneBaker where T : BaseCache {
        protected abstract T LoadCache { get; }
        protected T Cache => _cache ??= LoadCache;
        T _cache;

        public virtual void StartBaking() {
            Cache.Clear();
        }

        public virtual void Bake(SceneReference scene) { }

        public virtual void FinishBaking() {
            Cache.MarkBaked();
            _cache = null;
        }
    }
}
using System.Collections.Generic;
using Awaken.TG.Assets;

namespace Awaken.TG.Main.General.Caches {
    public interface ISceneCache<TData, TSource> where TData : ISceneCacheData<TSource> where TSource : ISceneCacheSource {
        List<TData> Data { get; }
    }
    
    public interface ISceneCacheData<T> where T : ISceneCacheSource {
        SceneReference SceneRef { get; }
        List<T> Sources { get; }
    }

    public interface ISceneCacheSource {
        
    }
}
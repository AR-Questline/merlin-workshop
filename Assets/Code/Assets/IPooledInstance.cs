using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Assets {
    public interface IPooledInstance {
        GameObject Instance { get; }
        bool InstanceLoaded { get; }

        async UniTaskVoid Return(float time) {
            var token = World.Services.Get<SceneService>().ActiveSceneExistenceToken;
            var duration = (int)(time * 1000);
            bool wasCanceled = await UniTask.Delay(duration, cancellationToken: token).SuppressCancellationThrow();
            if (!wasCanceled) {
                Return();
            } else {
                Release();
            }
        }

        void Return();
        void Release();
        void Invalidate();
    }
}
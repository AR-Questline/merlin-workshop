using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public class BackgroundUniTask : IBackgroundTask {
        public bool Done { get; private set; }

        public BackgroundUniTask(UniTask task) {
            try {
                AwaitForTask(task).Forget();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        async UniTaskVoid AwaitForTask(UniTask task) {
            await task;
            Done = true;
        }
    }
}

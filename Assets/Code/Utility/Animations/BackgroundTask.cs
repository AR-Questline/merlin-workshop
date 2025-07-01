using System;
using System.Threading.Tasks;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Utility.Animations {
    public class BackgroundTask : IBackgroundTask {
        public bool Done { get; private set; } = false;

        public BackgroundTask(Task task) {
            try {
                AwaitForTask(task).Forget();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        async UniTaskVoid AwaitForTask(Task task) {
            await task;
            Done = true;
        }
    }
}

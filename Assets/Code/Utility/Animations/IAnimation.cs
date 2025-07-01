using System;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public interface IAnimation : IBackgroundTask {
        void Start(MonoBehaviour behaviour);
        void ForceComplete();

        event Action OnDone;
    }
}

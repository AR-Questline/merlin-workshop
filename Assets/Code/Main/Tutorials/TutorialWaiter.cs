using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Tutorials {
    /// <summary>
    /// Waiter represents a running step in tutorial. It's execution is awaited by TutorialSequence.
    /// Waiter is also responsible for controlling whether the given step can be executed.
    /// </summary>
    public class TutorialWaiter {
        public string Key { get; }
        public Action Callback { get; set; }
        public bool WasPerformed { get; private set; }
        
        IEventListener Listener { get; set; }
        
        // It's enough to create empty waiter with no callbacks, to completely break tutorial execution
        public static TutorialWaiter BreakExecution => new(TutorialKeys.Break, null);

        public static TutorialWaiter TryCreate(string key, Action callback) {
            bool isForced = key == TutorialKeys.Forced;
            
            TutorialWaiter waiter;
            if (!isForced && TutorialKeys.IsConsumed(key)) {
                waiter = null;
            } else {
                waiter = new TutorialWaiter(key, callback);
            }
            
            if (TutorialMaster.DebugMode) {
                Log.Important?.Error($"TutorialWaiter {key} - {(waiter != null ? "Created" : "Not Created (Skipped)")}");
            }

            return waiter;
        }

        TutorialWaiter(string key, Action callback) {
            Key = key;
            Callback = callback;
        }

        public void AddListener(IEventListener listener) {
            Listener = listener;
        }

        public void Perform() {
            if (TutorialMaster.DebugMode && Key != TutorialKeys.Forced) {
                Log.Important?.Error($"TutorialWaiter {Key} - Finished");
            }
            
            TutorialKeys.Consume(Key);
            Callback?.Invoke();
            if (Listener != null) {
                World.EventSystem.RemoveListener(Listener);
            }
            WasPerformed = true;
        }
    }
}
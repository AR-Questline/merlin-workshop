// FROM: https://github.com/nickgravelyn/UnityToolbag/blob/master/Dispatcher/Dispatcher.cs

using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.Utility.Debugging;
using Awaken.Utility.Threads;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Utility.Threads {
    public class MainThreadDispatcher : MonoBehaviour {
        static MainThreadDispatcher s_instance;

        // We can't use the behaviour reference from other threads, so we use a separate bool
        // to track the instance so we can use that on the other threads.
        static bool s_instanceExists;

        static object s_lockObject = new object();
        static readonly Queue<Action> Actions = new Queue<Action>();

        /// <summary>
        /// Queues an action to be invoked on the main game thread.
        /// </summary>
        /// <param name="action">The action to be queued.</param>
        public static void InvokeAsync(Action action)
        {
            if (!s_instanceExists) {
                Log.Important?.Error("No Dispatcher exists in the scene. Actions will not be invoked!");
                return;
            }

            if (ThreadSafeUtils.IsMainThread) {
                // Don't bother queuing work on the main thread; just execute it.
                action();
            }
            else {
                lock (s_lockObject) {
                    Actions.Enqueue(action);
                }
            }
        }

        /// <summary>
        /// Queues an action to be invoked on the main game thread and blocks the
        /// current thread until the action has been executed.
        /// </summary>
        /// <param name="action">The action to be queued.</param>
        [UnityEngine.Scripting.Preserve]
        public static void Invoke(Action action)
        {
            if (!s_instanceExists) {
                Log.Important?.Error("No Dispatcher exists in the scene. Actions will not be invoked!");
                return;
            }

            bool hasRun = false;

            InvokeAsync(() =>
            {
                action();
                hasRun = true;
            });

            // Lock until the action has run
            while (!hasRun) {
                Thread.Sleep(5);
            }
        }

        void Awake()
        {
            if (s_instance) {
                DestroyImmediate(gameObject);
            }
            else {
                s_instance = this;
                s_instanceExists = true;
                DontDestroyOnLoad(this);
            }
        }

        void OnDestroy()
        {
            if (s_instance == this) {
                s_instance = null;
                s_instanceExists = false;
            }
        }

        void Update()
        {
            lock (s_lockObject) {
                while (Actions.Count > 0) {
                    Actions.Dequeue()();
                }
            }
        }
    }
}
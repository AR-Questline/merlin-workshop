using System;
using System.Diagnostics;
using System.Threading;

namespace Awaken.Utility.Threads {
    public static class ThreadSafeUtils {
        static Thread s_mainThread = Thread.CurrentThread;

        /// <summary>
        /// Gets a value indicating whether or not the current thread is the game's main thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread == s_mainThread;

        [Conditional("DEBUG")]
        public static void AssertMainThread() {
            if(!IsMainThread)
            {
                throw new ApplicationException("Call single thread api from not main thread");
            }
        }
    }
}
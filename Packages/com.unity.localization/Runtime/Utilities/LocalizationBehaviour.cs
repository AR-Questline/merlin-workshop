using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.Localization
{
    class LocalizationBehaviour : ComponentSingleton<LocalizationBehaviour>
    {
        Queue<(int frame, AsyncOperationHandle handle)> m_ReleaseQueue = new Queue<(int, AsyncOperationHandle)>();

        const long k_MaxMsPerUpdate = 10;
        const bool k_DisableThrottling = false;

        protected override string GetGameObjectName() => "Localization Resource Manager";

        /// <summary>
        /// To prevent you having to explicitly release operations, Unity does this automatically a frame after the operation is completed.
        /// If you plan to keep hold of a reference, call <see cref="AsyncOperationHandle.Acquire"/>, and <see cref="AsyncOperationHandle.Release"/> when it's finished.
        /// </summary>
        /// <param name="handle"></param>
        public static void ReleaseNextFrame(AsyncOperationHandle handle) => Instance.DoReleaseNextFrame(handle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long TimeSinceStartupMs() => (long)(Time.realtimeSinceStartup * 1000.0f);

        void DoReleaseNextFrame(AsyncOperationHandle handle)
        {
            enabled = true;
            m_ReleaseQueue.Enqueue((Time.frameCount, handle));
        }

        void LateUpdate()
        {
            var currentFrame = Time.frameCount;
            long currentTime = TimeSinceStartupMs();
            long maxTime = currentTime + k_MaxMsPerUpdate;
            while (m_ReleaseQueue.Count > 0 && m_ReleaseQueue.Peek().frame < currentFrame)
            {
                currentTime = TimeSinceStartupMs();
                if (!k_DisableThrottling && currentTime >= maxTime)
                {
                    // We spent too much time on this frame, we break for now, we'll resume next frame
                    break;
                }

                var item = m_ReleaseQueue.Dequeue();
                AddressablesInterface.SafeRelease(item.handle);
            }

            if (m_ReleaseQueue.Count == 0)
                enabled = false;
        }

        public static void ForceRelease()
        {
            foreach(var r in Instance.m_ReleaseQueue)
            {
                AddressablesInterface.SafeRelease(r.handle);
            }
            Instance.m_ReleaseQueue.Clear();
        }
    }
}

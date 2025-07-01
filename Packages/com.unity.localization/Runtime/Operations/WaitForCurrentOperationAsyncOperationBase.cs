using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Operations
{
    /// <summary>
    /// Extended version of <seealso cref="AsyncOperationBase{TObject}"/> which includes generic <seealso cref="InvokeWaitForCompletion"/> support.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    abstract class WaitForCurrentOperationAsyncOperationBase<TObject> : AsyncOperationBase<TObject>
    {
        protected AsyncOperationHandle CurrentOperation { get; set; }

        public AsyncOperationHandle Dependency { get; set; }

        bool m_Waiting;

        protected override bool InvokeWaitForCompletion()
        {
            Debug.Assert(!m_Waiting, "Calling WaitForCompletion on an operation that is already waiting.");

            if (!IsRunning || m_Waiting)
                return true;

            try
            {
                m_Waiting = true;

                // If we have dependencies then we need to wait for them.
                if (!HasExecuted && Dependency.IsValid())
                {
                    Dependency.WaitForCompletion();

                    // Its possible that the operation was released after use so we ignore invalid operations.
                    if (Dependency.IsValid() && Dependency.Status == AsyncOperationStatus.Failed)
                    {
                        Complete(default, false, $"Dependency `{Handle.DebugName}` failed to complete.");
                        return true;
                    }

                    // Its possible that an operation will complete instantly once all dependencies have finished so we return and start again.
                    return false;
                }

                if (!CurrentOperation.IsValid())
                {
                    Complete(default, false, $"Expected to have a current operation to wait on. Can not complete {ToString()}.");
                    return true;
                }

                CurrentOperation.WaitForCompletion();
                return false;
            }
            finally
            {
                m_Waiting = false;
            }
        }

        protected override void Destroy()
        {
            base.Destroy();
            Dependency = default;
            CurrentOperation = default;

            // HasExecuted does not get reset so we need to do it. https://unity.slack.com/archives/C8Z80RV4K/p1620124726080400
            HasExecuted = false;
        }
    }
}

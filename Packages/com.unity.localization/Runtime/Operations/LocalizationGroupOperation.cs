using UnityEngine.Pool;

namespace UnityEngine.Localization.Operations
{
    /// <summary>
    /// Group operation with 2 changes:
    /// - We use a for loop in InvokeWaitForCompletion instead of foreach to prevent this issue https://unity.slack.com/archives/C8Z80RV4K/p1662643037333139
    /// - We pool and reuse the operation.
    /// </summary>
    class LocalizationGroupOperation : ResourceManagement.AsyncOperations.GroupOperation
    {
        public static readonly ObjectPool<LocalizationGroupOperation> Pool = new ObjectPool<LocalizationGroupOperation>(
            () => new LocalizationGroupOperation(), collectionCheck: false);

        protected override bool InvokeWaitForCompletion()
        {
            //If Result is null then we've auto released and need to return
            if (IsDone || Result == null)
                return true;

            // Using a for loop instead!
            for (int i = 0; i < Result.Count; ++i)
            {
                Result[i].WaitForCompletion();
                if (Result == null)
                    return true;
            }

            m_RM?.Update(Time.unscaledDeltaTime);
            if (!IsDone && Result != null)
                Execute();
            m_RM?.Update(Time.unscaledDeltaTime);
            return IsDone;
        }

        protected override void Destroy()
        {
            Pool.Release(this);
            base.Destroy();
        }
    }
}

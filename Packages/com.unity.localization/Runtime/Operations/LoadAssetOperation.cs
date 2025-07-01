using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Operations
{
    class LoadAssetOperation<TObject> : WaitForCurrentOperationAsyncOperationBase<TObject> where TObject : Object
    {
        readonly Action<AsyncOperationHandle<TObject>> m_AssetLoadedAction;
        AsyncOperationHandle<LocalizedAssetDatabase.TableEntryResult> m_TableEntryOperation;
        AsyncOperationHandle<TObject> m_LoadAssetOperation;
        bool m_AutoRelease;

        public static readonly ObjectPool<LoadAssetOperation<TObject>> Pool = new ObjectPool<LoadAssetOperation<TObject>>(
            () => new LoadAssetOperation<TObject>(), collectionCheck: false);

        public LoadAssetOperation()
        {
            m_AssetLoadedAction = AssetLoaded;
        }

        public void Init(AsyncOperationHandle<LocalizedAssetDatabase.TableEntryResult> loadTableEntryOperation, bool autoRelease)
        {
            m_TableEntryOperation = loadTableEntryOperation;
            AddressablesInterface.Acquire(m_TableEntryOperation);
            m_AutoRelease = autoRelease;
        }

        protected override void Execute()
        {
            if (m_TableEntryOperation.Status != AsyncOperationStatus.Succeeded)
            {
                Complete(null, false, "Load Table Entry Operation Failed");
                AddressablesInterface.Release(m_TableEntryOperation);
                return;
            }

            if (m_TableEntryOperation.Result.Table == null || m_TableEntryOperation.Result.Entry == null)
            {
                // Missing entries are treated as null objects
                CompleteAndRelease(default, true, null);
                return;
            }

            m_LoadAssetOperation = m_TableEntryOperation.Result.Table.GetAssetAsync<TObject>(m_TableEntryOperation.Result.Entry);
            AddressablesInterface.Acquire(m_LoadAssetOperation);
            if (m_LoadAssetOperation.IsDone)
                AssetLoaded(m_LoadAssetOperation);
            else
            {
                CurrentOperation = m_LoadAssetOperation;
                m_LoadAssetOperation.Completed += m_AssetLoadedAction;
            }
        }

        void AssetLoaded(AsyncOperationHandle<TObject> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
                CompleteAndRelease(null, false, "GetAssetAsync failed to load the asset.");
            else
                CompleteAndRelease(handle.Result, true, null);
        }

        public void CompleteAndRelease(TObject result, bool success, string errorMsg)
        {
            Complete(result, success, errorMsg);
            AddressablesInterface.Release(m_TableEntryOperation);

            if (m_AutoRelease && LocalizationSettings.Instance.IsPlaying)
            {
                // We need internal access for Handle here.
                LocalizationBehaviour.ReleaseNextFrame(Handle);
            }
        }

        protected override void Destroy()
        {
            AddressablesInterface.ReleaseAndReset(ref m_LoadAssetOperation);
            base.Destroy();
            Pool.Release(this);
        }
    }
}

using System;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Operations
{
    class LoadSubAssetOperation<TObject> : WaitForCurrentOperationAsyncOperationBase<TObject> where TObject : Object
    {
        readonly Action<AsyncOperationHandle<TObject>> m_AssetLoadedAction;

        AsyncOperationHandle<TObject> m_AssetOperation;
        AsyncOperationHandle<Object[]> m_PreloadOperations;
        string m_Address;
        bool m_IsSubAsset;
        string m_SubAssetName;

        public static readonly ObjectPool<LoadSubAssetOperation<TObject>> Pool = new ObjectPool<LoadSubAssetOperation<TObject>>(
            () => new LoadSubAssetOperation<TObject>(), collectionCheck: false);

        public LoadSubAssetOperation()
        {
            m_AssetLoadedAction = AssetLoaded;
        }

        public void Init(AsyncOperationHandle<Object[]> preloadOperations, string address, bool isSubAsset, string subAssetName)
        {
            Dependency = preloadOperations;
            m_PreloadOperations = preloadOperations;

            if (m_PreloadOperations.IsValid())
                AddressablesInterface.Acquire(m_PreloadOperations);
            m_Address = address;
            m_IsSubAsset = isSubAsset;
            m_SubAssetName = subAssetName;
        }

        protected override void Execute()
        {
            // Do we have preload data?
            if (m_PreloadOperations.IsValid())
            {
                if (m_PreloadOperations.Status != AsyncOperationStatus.Succeeded)
                {
                    Complete(null, false, m_PreloadOperations.OperationException.Message);
                    return;
                }

                // Extract the asset from the array of preloaded sub objects.
                foreach (var obj in m_PreloadOperations.Result)
                {
                    if (obj is TObject target)
                    {
                        if (m_IsSubAsset && m_SubAssetName != obj.name)
                            continue;

                        Complete(target, true, null);
                        return;
                    }
                }
            }

            m_AssetOperation = AddressablesInterface.LoadAssetFromGUID<TObject>(m_Address);
            if (m_AssetOperation.IsDone)
            {
                AssetLoaded(m_AssetOperation);
            }
            else
            {
                CurrentOperation = m_AssetOperation;
                m_AssetOperation.Completed += m_AssetLoadedAction;
            }
        }

        void AssetLoaded(AsyncOperationHandle<TObject> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
                Complete(null, false, string.Format("Failed to load sub-asset {0} from the address {1}.", m_SubAssetName, m_Address));
            else
                Complete(handle.Result, true, null);
        }

        protected override void Destroy()
        {
            AddressablesInterface.ReleaseAndReset(ref m_PreloadOperations);
            AddressablesInterface.ReleaseAndReset(ref m_AssetOperation);

            base.Destroy();
            Pool.Release(this);
        }
    }
}

using System;

namespace Awaken.TG.Assets {
    /// <summary>
    /// Holds reference instance, allows to easily change reference.
    /// </summary>
    public class ReferenceHolder<T> where T : class {
        public ARAssetReference LoadedReference { get; private set; }
        public ARAssetReference CurrentReference { get; private set; }
        public T Instance { [UnityEngine.Scripting.Preserve] get; private set; }
        public event Action<T> OnLoad;

        public void SetReference(ShareableARAssetReference reference) {
            if (reference?.AssetGUID == CurrentReference?.Address) return;
            if (reference == null || !reference.IsSet) {
                Instance = null;
                OnLoad?.Invoke(null);
                LoadedReference?.ReleaseAsset();
                LoadedReference = CurrentReference = null;
            } else {
                CurrentReference = reference.Get();
                CurrentReference.LoadAsset<T>().OnComplete(GetLoadHandle(CurrentReference));
            }
        }

        [UnityEngine.Scripting.Preserve]
        public void Release() {
            SetReference(null);
        }

        Action<ARAsyncOperationHandle<T>> GetLoadHandle(ARAssetReference reference) {
            return handle => {
                if (CurrentReference == reference) {
                    LoadedReference = CurrentReference;
                    Instance = handle.Result;
                    OnLoad?.Invoke(handle.Result);
                } else {
                    reference.ReleaseAsset();
                }
            };
        }
    }
}

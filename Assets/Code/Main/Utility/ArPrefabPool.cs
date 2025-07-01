using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Utility {
    public class ArPrefabPool<T> : IDisposable where T : Component {
        readonly List<T> _list;
        readonly Action<T> _actionOnGet;
        readonly Action<T> _actionOnRelease;
        readonly int _maxSize;
        ARAsyncOperationHandle<GameObject> _assetHandle;
        Transform _poolParent;
        T _prefab;
        
        [UnityEngine.Scripting.Preserve] public int CountInactive => _list.Count;
        [UnityEngine.Scripting.Preserve] public bool IsValid => _assetHandle.IsValid();

        public ArPrefabPool(Transform poolParent, ARAssetReference prefabRef, Action<T> onGet, Action<T> onRelease, int defaultCapacity, int maxSize) {
            _assetHandle = prefabRef.LoadAsset<GameObject>();
            _assetHandle.OnComplete(handle => _prefab = handle.Result.GetComponent<T>());
            _list = new List<T>(defaultCapacity);
            _maxSize = maxSize;
            _actionOnGet = onGet ?? delegate { };
            _actionOnRelease = onRelease ?? delegate { };
            _poolParent = poolParent;
        }

        [UnityEngine.Scripting.Preserve]
        public T Get() {
            T obj;
            if (_list.Count > 0) {
                int index = _list.Count - 1;
                obj = _list[index];
                _list.RemoveAt(index);
            } else {
                obj = Object.Instantiate(_prefab);
            }
            
            obj.transform.SetParent(null);
            _actionOnGet(obj);
            return obj;
        }
        
        [UnityEngine.Scripting.Preserve]
        /// <param name="t">Will be set to null</param>
        public void Release(ref T t) {
            _actionOnRelease(t);
            if (_list.Count > _maxSize) {
                Object.Destroy(t);
            } else {
                t.transform.SetParent(_poolParent);
                _list.Add(t);
            }

            t = null;
        }
        
        public void Dispose() {
            foreach (var t in _list) {
                Object.Destroy(t);
            }

            _prefab = null;
            _list.Clear();
            _assetHandle.Release();
            _assetHandle = default;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Awaken.Utility.Collections {
    public struct PooledList<T> : IDisposable {
        public static PooledList<T> Empty = new List<T>(0);
        public List<T> value;

        public PooledList(List<T> value) {
            this.value = value;
        }
        
        public void Release() {
            Release(value);
        }

        public bool CheckContainsAndRelease(T checkValue) {
            bool result = value.Contains(checkValue);
            Release();
            return result;
        }

        public bool CheckIsEmptyAndRelease() {
            bool result = value.Count == 0;
            Release();
            return result;
        }
        public static void Get(out PooledList<T> pooledList) {
            ListPool<T>.Get(out var list);
            pooledList = list;
        }

        public static void Release(List<T> list) {
            ListPool<T>.Release(list);
        }
        
        public static implicit operator PooledList<T>(List<T> list) => new(list);
        public static implicit operator List<T>(PooledList<T> pooledList) => pooledList.value;
        public void Dispose() {
            Release();
        }
    }
}
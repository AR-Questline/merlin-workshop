using System;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public struct quaternionHalf {
        [SerializeField] half4 _xyzw;

        public quaternionHalf(quaternion quaternion) {
            var value = quaternion.value;
            _xyzw = new(new(value.x), new(value.y), new(value.z), new(value.w));
        }

        public static implicit operator quaternionHalf(quaternion quaternion) => new(quaternion);
        public static implicit operator quaternionHalf(Quaternion quaternion) => new(quaternion);
        public static implicit operator quaternion(quaternionHalf quaternion) =>
            new(quaternion._xyzw.x, quaternion._xyzw.y, quaternion._xyzw.z, quaternion._xyzw.w);
        public static implicit operator Quaternion(quaternionHalf quaternion) =>
            new(quaternion._xyzw.x, quaternion._xyzw.y, quaternion._xyzw.z, quaternion._xyzw.w);
    }
}

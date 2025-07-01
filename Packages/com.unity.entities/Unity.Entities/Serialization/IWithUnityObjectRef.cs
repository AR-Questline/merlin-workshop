using System;

namespace Unity.Entities.Serialization {
    public interface IWithUnityObjectRef {
        Type Type { get; }
        UnityEngine.Object Object { get; }
    }
}

using UnityEngine;

namespace Awaken.CommonInterfaces.Assets {
    public interface IEditorOnlyTransform {
        Transform transform { get; }
        GameObject gameObject { get; }
        bool PreserveChildren { get; }
    }
}
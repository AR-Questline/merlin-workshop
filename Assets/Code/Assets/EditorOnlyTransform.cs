using Awaken.CommonInterfaces.Assets;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Assets {
    [DisallowMultipleComponent, InfoBox("This component and its Transform will be removed in builds if no other components are present on this GameObject.")]
    public class EditorOnlyTransform : MonoBehaviour, IEditorOnlyTransform {
        [field: SerializeField, FormerlySerializedAs("JustThis")] public bool PreserveChildren { get; private set; }
    }
}

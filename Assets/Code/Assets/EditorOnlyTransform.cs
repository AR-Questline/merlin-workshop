using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Assets;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Assets {
    [DisallowMultipleComponent]
    public class EditorOnlyTransform : MonoBehaviour, IEditorOnlyTransform {
        [InfoBox("Transform will be removed if no other components are present here")]
        [field: SerializeField, FormerlySerializedAs("JustThis")] public bool PreserveChildren { get; private set; }
    }
}

using Awaken.CommonInterfaces.Assets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Unity.HLODSystem {
    public class HlodTargetOptions : MonoBehaviour, IEditorOnlyMonoBehaviour {
#if UNITY_EDITOR
        [SerializeField] bool _excludeFromHlod = true;
        [SerializeField, ShowIf(nameof(_excludeFromHlod))] bool _cullWithHlod = true;

        public bool ExcludeFromHlod => _excludeFromHlod;
        public bool CullWithHlod => _cullWithHlod;
#endif
    }
}

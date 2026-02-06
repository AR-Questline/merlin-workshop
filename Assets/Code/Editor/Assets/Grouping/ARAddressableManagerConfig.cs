using Awaken.TG.Editor.Assets.Grouping.Modifiers;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Grouping {
    [CreateAssetMenu(fileName = "ARAddressableManagerConfig", menuName = "TG/AR Addressable Manager Config")]
    public class ARAddressableManagerConfig : ScriptableObject {
        public AssetGroupMostUsagesSplitModifier mostUsagesSplitModifier;
        [HideInInspector]
        public AssetGroupTypeSplitModifier typeSplitModifier = new();
        [HideInInspector]
        public AssetGroupPrefabsExcludeModifier prefabsExcludeModifier = new();
        [HideInInspector]
        public AssetGroupScenesExcludeModifier scenesExcludeModifier = new();
        [HideInInspector]
        public AssetGroupUsagesSplitModifier commonUsagesModifier = new();
        [HideInInspector]
        public AssetGroupUnityAssetsExcludeModifier unityAssetsExcludeModifier = new();
        public AssetGroupSplitModifier splitModifier;
        public AssetGroupMergeModifier mergeModifier;
        
        public bool assignGroups;
    }
}
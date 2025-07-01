using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Awaken.TG.Editor.Animations {
    public static class ARStateToAnimationMappingEditorUtils {
        [MenuItem("Assets/TG/ConvertAnimationMapping/ToInteractionMapping", true)]
        static bool IsBaseAnimationMapping() {
            Debug.Log(Selection.activeObject is ARStateToAnimationMapping);
            return Selection.activeObject.GetType() == typeof(ARStateToAnimationMapping);
        }
        
        [MenuItem("Assets/TG/ConvertAnimationMapping/ToInteractionMapping")]
        static void ConvertToInteractionAnimationMapping() {
            ConvertToOtherType<ARStateToInteractionAnimationMapping>((ARStateToAnimationMapping) Selection.activeObject);
        }
        
        static void ConvertToOtherType<T>(ARStateToAnimationMapping baseSO) where T : ARStateToAnimationMapping {
            string assetPath = AssetDatabase.GetAssetPath(baseSO);
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var newSO = ScriptableObject.CreateInstance<T>();
            
            EditorUtility.CopySerialized(baseSO, newSO);
            newSO.entries = baseSO.entries;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            var oldEntry = settings.FindAssetEntry(assetGuid);
            var oldAddress = oldEntry.address;
            var oldGroup = oldEntry.parentGroup;
            
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(newSO, assetPath);
            
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(assetGuid, oldGroup);
            entry.address = oldAddress;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}

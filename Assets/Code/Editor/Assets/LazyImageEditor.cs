using Awaken.TG.Assets;
using Awaken.TG.MVC;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Awaken.TG.Editor.Assets {
    [CustomEditor(typeof(LazyImage))]
    public class LazyImageEditor : UnityEditor.Editor {
        LazyImage Target => target as LazyImage;
        
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            EditorGUILayout.BeginHorizontal();
            
            TryAutoAssign();

            if (GUILayout.Button("Load")) {
                Load(Target);
            }

            if (GUILayout.Button("Unload")) {
                Unload(Target);
            }
            EditorGUILayout.EndHorizontal();
        }

        void TryAutoAssign() {
            if (Target.image == null) {
                Target.image = Target.gameObject.GetComponent<Image>();
                EditorUtility.SetDirty(Target);
            }
            
            if (Target.image?.sprite != null && (Target.arSpriteReference == null || string.IsNullOrWhiteSpace(Target.arSpriteReference.Address)) ) {
                var guid = AddressableHelper.AddEntry(new AddressableEntryDraft.Builder(Target.image.sprite).Build());
                Target.arSpriteReference = new ARAssetReference(guid);
                EditorUtility.SetDirty(Target);
            }
        }

        public static void Load(LazyImage target) {
            target.image.sprite = AddressableHelper.FindFirstEntry<Sprite>(target.arSpriteReference);
            EditorUtility.SetDirty(target.image);
        }

        public static void Unload(LazyImage target) {
            target.image.sprite = null;
            EditorUtility.SetDirty(target.image);
        }
    }
}
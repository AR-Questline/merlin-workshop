using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Main.Utility.VSDatums;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums.TypeInstances {
    public class VSDatumAssetDrawer<T> : VSDatumTypeInstanceDrawer where T : Object {
        public static VSDatumTypeInstanceDrawer Instance { get; private set; }

        readonly ARAssetReferenceSettingsAttribute _settings;

        static VSDatumAssetDrawer() {
            VSDatumAssetDrawer<GameObject>.Instance = new VSDatumAssetDrawer<GameObject>(new PrefabAssetReferenceAttribute());
        }

        VSDatumAssetDrawer(ARAssetReferenceSettingsAttribute settings) {
            _settings = settings;
        }

        public override void Draw(in Rect rect, SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var previousObject = value.Asset?.EditorLoad<T>();
            var newObject = EditorGUI.ObjectField(rect, previousObject, typeof(T), false) as T;
            if (newObject != previousObject) {
                value = new VSDatumValue { Asset = Create(newObject) };
                changed = true;
            } else {
                changed = false;
            }
        }

        public override void DrawInLayout(SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var previousObject = value.Asset?.EditorLoad<T>();
            var newObject = EditorGUILayout.ObjectField(previousObject, typeof(T), false) as T;
            if (newObject != previousObject) {
                value = new VSDatumValue { Asset = Create(newObject) };
                changed = true;
            } else {
                changed = false;
            }
        }
        
        ARAssetReference Create(T asset) {
            var reference = new ARAssetReference();
            if (asset != null) {
                ARAssetReferencePropertyDrawer.AssignAsset(reference, asset, _settings);
            }
            return reference;
        }
    }
}
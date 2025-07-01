using Awaken.TG.Graphics.Scene;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics {
    public class SceneConfigsWindow : OdinEditorWindow {
        public const string SceneConfigAssetPath = "Assets/Data/Settings/SceneConfigs.asset";
        [SerializeField, HideInInspector] string path;
        Object _asset;
        UnityEditor.Editor _assetEditor;
        
        void SetPath(string value) {
            path = value;
            OpenAsset();
        }

        void OpenAsset() {
            if (_asset != null) {
                Destroy(_asset);
                Destroy(_assetEditor);
            }
            _asset = AssetDatabase.LoadAssetAtPath<SceneConfigs>(path);
            _assetEditor = UnityEditor.Editor.CreateEditor(_asset);
        }

        public static SceneConfigsWindow CreateOrFocus(string assetPath) {
            if (EditorWindow.HasOpenInstances<SceneConfigsWindow>()) {
                EditorWindow.FocusWindowIfItsOpen<SceneConfigsWindow>();
                
                var sceneConfigsWindow = EditorWindow.GetWindow<SceneConfigsWindow>();
                if (sceneConfigsWindow.path.IsNullOrWhitespace() || sceneConfigsWindow.path != assetPath || sceneConfigsWindow._asset == null) {
                    sceneConfigsWindow.SetPath(assetPath);
                }
                return sceneConfigsWindow;
            }

            var window = CreateWindow<SceneConfigsWindow>("Scene configs");
            window.SetPath(assetPath);
            return window;
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (_asset == null && !path.IsNullOrWhitespace()) {
                OpenAsset();
            }
        }

        protected override void OnImGUI() {
            GUI.enabled = false;
            _asset = EditorGUILayout.ObjectField("Asset", _asset, _asset.GetType(), false);
            GUI.enabled = true;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _assetEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
            base.OnImGUI();
        }
    }
}
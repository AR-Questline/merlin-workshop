using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Graphics.VisualsPickerTool;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Graphics.VisualsPickerTool {
    [CustomEditor(typeof(VisualsPicker))]
    public class VisualsPickerEditor : OdinEditor {
        const string TargetGroup = "Locations";
        const int GridSize = 100;
        static VisualsPicker s_current;

        GUIStyle _gridStyle;

        VisualsPicker _target;
        VisualsKitTypes _currentKit;
        GUIContent[] _previewTextures;

        protected override void OnEnable() {
            base.OnEnable();
            _target = (VisualsPicker) target;
            _target.RefreshAssetGroup();
            s_current = _target;
            LoadPreviews();
        }

        void LoadPreviews() {
            if (_target.CanChangeAssets()) {
                _currentKit = _target.VisualKit;
                var textures = new List<GUIContent>();
                foreach (ARAssetReference assetReference in _target.CurrentGroup.Assets) {
                    if (assetReference.IsSet) {
                        var obj = assetReference.LoadAsset<Object>().WaitForCompletion();
                        textures.Add(new GUIContent(obj.name, AssetPreview.GetAssetPreview(obj)));
                    }
                }

                _previewTextures = textures.ToArray();
            }
        }

        [Shortcut("VisualsPicker/Next Visual", KeyCode.Period)]
        static void NextVisual() {
            if (s_current != null) {
                if (s_current.gameObject == Selection.activeGameObject && s_current.CanChangeAssets()) {
                    s_current.NextVisual();
                } else {
                    s_current = null;
                }
            }
        }
        
        [Shortcut("VisualsPicker/Previous Visual", KeyCode.Comma)]
        static void PreviousVisual() {
            if (s_current != null) {
                if (s_current.gameObject == Selection.activeGameObject && s_current.CanChangeAssets()) {
                    s_current.PreviousVisual();
                } else {
                    s_current = null;
                }
            }
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (_target.CurrentGroup == null) {
                return;
            }

            if (_target.CanSpawnAssets()) {
                DrawAssets();
            } else {
                DrawDropBox();
            }
        }

        void DrawAssets() {
            if (!_target.IsAssetGroupSet()) {
                EditorGUILayout.HelpBox("No assets found for chosen Visuals Kit", MessageType.Warning, true);
                return;
            }

            if (_currentKit != _target.VisualKit) {
                LoadPreviews();
            }

            _gridStyle ??= new GUIStyle(GUI.skin.button) {
                imagePosition = ImagePosition.ImageAbove,
                fixedHeight = GridSize,
                wordWrap = true
            };

            int columns = Mathf.FloorToInt(EditorGUIUtility.currentViewWidth / GridSize);

            int newIndex = GUILayout.SelectionGrid(_target.CurrentIndex, _previewTextures, columns, _gridStyle);
            if (newIndex != _target.CurrentIndex) {
                _target.SetByIndex(newIndex);
            }
        }
        
        void DrawDropBox() {
            
            EditorGUILayout.HelpBox("\nDROP ASSETS HERE TO ADD THEM TO THE LIST\n", MessageType.Info, true);
            ManageDragAndDrop(GUILayoutUtility.GetLastRect());
        }

        void ManageDragAndDrop(Rect dropRect) {
            if (Event.current.type == EventType.DragUpdated && dropRect.Contains(Event.current.mousePosition)) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                Event.current.Use();
            } else if (Event.current.type == EventType.DragPerform && dropRect.Contains(Event.current.mousePosition)) {
                foreach (Object objectToAdd in DragAndDrop.objectReferences) {
                    AssignAsset(objectToAdd);
                }
                DragAndDrop.AcceptDrag();
            }
        }
        
        public void AssignAsset(Object objectToAdd) {
            
            var assetPath = AssetDatabase.GetAssetPath(objectToAdd);
            if (objectToAdd == null) {
                EditorUtility.DisplayDialog("Invalid object", $"Can not add asset to addressables because asset is null", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(assetPath)) {
                EditorUtility.DisplayDialog("Invalid object", $"Can not add asset {objectToAdd.name} to addressables because can not obtain asset path", "OK");
                return;
            }
            if ( AddressableHelperEditor.IsResourcesPath(assetPath)) {
                EditorUtility.DisplayDialog("Invalid object", $"Can not add asset {objectToAdd.name} to addressables because asset lives in Resources directory. Move asset to proper directory and try again.", "OK");
                return;
            }

            var guid = AddressableHelper.AddEntry(
                new AddressableEntryDraft.Builder(objectToAdd)
                    .InGroup(TargetGroup)
                    .Build());
            
            _target.AddNewAsset(new ARAssetReference(guid));
            EditorUtility.SetDirty(_target);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Templates;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.TG.Editor.Assets.Templates {
    
    public class AddressableTemplatesWindow : EditorWindow {
        
        readonly string _windowTitle = "Addressable Templates Creator";
        readonly Vector2 _windowSize = new Vector2(300, 450);
        
        [SerializeField] TreeViewState treeViewState;

        TemplatesTreeView _treeView;
        
        Rect TreeViewRect => new(10, 40, position.width-20, position.height-60);
        Rect ButtonRect => new(20f, 10f, 200f, 20f);
        List<string> _guids;

        void Init (List<string> guids) {
	        minSize = _windowSize;
            titleContent = new GUIContent(_windowTitle);

            treeViewState ??= new TreeViewState();

			RefreshTree(guids);
        }

        void RefreshTree(List<string> guids) {
	        var header = CreateDefaultMultiColumnHeaderState();
	        _guids = guids;
	        _treeView = new TemplatesTreeView(treeViewState, new MultiColumnHeader(header), guids);
	        _treeView.ExpandAll();
        }

        void OnGUI () { 
	        if (_treeView.IsEmpty()) {
		        GUILayout.Label("No non-addressable templates found");
	        } else {
		        ButtonArea(ButtonRect);
		        _treeView.OnGUI(TreeViewRect);
	        }
        }

        void ButtonArea (Rect rect) {
	        GUILayout.BeginArea (rect);
	        if (GUILayout.Button("CONVERT")) {
		        AddressableTemplatesCreator.Convert(_treeView);
		        RefreshTree(_guids);
	        }
	        GUILayout.EndArea();
        }
        
        [MenuItem("TG/Addressables/Addressable Templates Creator")]
        static void OpenConvertAll() {
            AddressableTemplatesWindow window = GetWindow<AddressableTemplatesWindow>();
            window.Init(GetTemplatesFromDirectory("Assets/"));
        }
        
        [MenuItem("Assets/TG/Convert Templates")]
        static void OpenConvertSelected() {
	        AddressableTemplatesWindow window = GetWindow<AddressableTemplatesWindow>();
	        window.Init(GetGUIDsFromSelection(Selection.objects));
        }
        
        
        static List<string> GetGUIDsFromSelection(Object[] selectedObjects) {
            var result = new List<string>();
            foreach (Object selectedObject in selectedObjects) {
                result.AddRange(GetGUIDsFromSelectedObject(selectedObject));
            }

            return result;
        }

        static List<string> GetGUIDsFromSelectedObject(Object selectedObject) {
            var result = new List<string>();
            if (selectedObject is GameObject go) {
                if (go.GetComponent<ITemplate>() != null) {
                    result.Add(AssetsUtils.ObjectToGuid(selectedObject));
                }
            } else if (selectedObject is ITemplate template) {
                result.Add(AssetsUtils.ObjectToGuid(selectedObject));
            } else {
                result.AddRange(GetTemplatesFromDirectory(AssetDatabase.GetAssetPath(selectedObject)));
            }

            return result;
        }

        static List<string> GetTemplatesFromDirectory(string directory) {
            return GetPrefabs(directory).Concat(GetScriptableObjects(directory)).ToList();
        }
        
        static List<string> GetPrefabs (string path) {
            var assets = AssetDatabase.FindAssets("t:GameObject", new[] {path});
            var result = new List<string>();
            foreach (string guid in assets) {
                if (AddressableHelper.IsAddressable(guid)) {
                    continue;
                }
                Object asset = AssetsUtils.LoadAssetByGuid<Object>(guid);
                if (asset is GameObject go) {
                    if (go.GetComponent<ITemplate>() != null) {
                        result.Add(guid);
                    }
                }
            }
            return result;
        }

        static List<string> GetScriptableObjects(string path) {
            var assets = AssetDatabase.FindAssets("t:ScriptableObject", new[] {path});
            var result = new List<string>();

            foreach (string guid in assets) {
                if (AddressableHelper.IsAddressable(guid)) {
                    continue;
                }
                Object asset = AssetsUtils.LoadAssetByGuid<Object>(guid);
                if (asset is ITemplate) {
                    result.Add(guid);
                }
            }

            return result;
        }

        static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState() {
			var columns = new[] {
				new MultiColumnHeaderState.Column {
					headerContent = new GUIContent("Asset"),
					contextMenuText = "Asset",
					headerTextAlignment = TextAlignment.Center,
					sortedAscending = true,
					sortingArrowAlignment = TextAlignment.Right,
					width = 500, 
					minWidth = 30,
					maxWidth = 10000,
					autoResize = false,
					allowToggleVisibility = true
				},
				new MultiColumnHeaderState.Column {
					headerTextAlignment = TextAlignment.Center,
					sortedAscending = true,
					sortingArrowAlignment = TextAlignment.Right,
					width = 30, 
					minWidth = 30,
					maxWidth = 60,
					autoResize = false,
					allowToggleVisibility = true
				},
				new MultiColumnHeaderState.Column {
					headerContent = new GUIContent("Destination"),
					contextMenuText = "Destination",
					headerTextAlignment = TextAlignment.Center,
					sortedAscending = true,
					sortingArrowAlignment = TextAlignment.Right,
					width = 500, 
					minWidth = 30,
					maxWidth = 10000,
					autoResize = false,
					allowToggleVisibility = true
				}
			};

			var state =  new MultiColumnHeaderState(columns);
			return state;
		}
    }
}
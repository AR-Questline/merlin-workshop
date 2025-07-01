using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Graphics;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
	class SceneWindow : OdinMenuEditorWindow {
		[MenuItem("Window/Scenes")]
		public static void ShowWindow() {
			EditorWindow.GetWindow(typeof(SceneWindow), false, "Scenes");
		}

		bool ShowAllScenes {
			get => EditorPrefs.GetBool("ScenesWindow:ShowAll", false);
			set => EditorPrefs.SetBool("ScenesWindow:ShowAll", value);
		}

		// bool SearchFunction(OdinMenuItem arg) {
		// 	if (arg.Value is not string scene) return false;
		// 	
		// 	scene = Path.GetFileNameWithoutExtension(scene);
		// 	return scene.Contains(MenuTree.Config.SearchTerm, 
		// 		StringComparison.InvariantCultureIgnoreCase);
		// }
		//
		// static void OnSelectionConfirmed(OdinMenuTreeSelection obj) {
		// 	var scene = obj.SelectedValue as string;
		// 	if (scene == null) return; //Folders have obj as null
		// 	if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
		// 		EditorSceneManager.OpenScene(scene);
		// 	}
		// }
		//
		// protected override OdinMenuTree BuildMenuTree() {
		// 	var tree = new OdinMenuTree();
		// 	tree.Config.SearchFunction = SearchFunction;
		// 	tree.Config.DrawSearchToolbar = true;
		// 	// _tree.Config.ConfirmSelectionOnDoubleClick = false;
		// 	// _tree.Config.SelectMenuItemsOnMouseDown = true;
		// 	
		// 	bool showAll = ShowAllScenes;
		// 	string[] paths = Directory.GetFiles(Application.dataPath + (showAll ? "" : "\\Scenes"), "*.unity", SearchOption.AllDirectories);
		// 	foreach (string path in paths) {
		// 		var sanitized = path.Replace('\\', '/');
		// 		string directory = sanitized.Replace(".unity", "").Replace(Application.dataPath + "/", "");
		// 		if (!showAll) {
		// 			directory = directory["Scenes/".Length..];
		// 		}
		// 		tree.Add(directory, sanitized);
		// 	}
		// 	
		// 	tree.SortMenuItemsByName(false);
		//
		// 	foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
		// 		string sceneName = Path.GetFileNameWithoutExtension(scene.path);
		// 		tree.Add("BuildScenes/" + sceneName, scene.path);
		// 	}
		// 	tree.EnumerateTree().Where(item => item.Value != null).ForEach(x => x.IconGetter = () => GUIHelper.GetAssetThumbnail(null, typeof(SceneAsset), false));
		// 	
		// 	// Style configs
		// 	//  - Folder triangles to the left
		// 	//  - BuildScenes folder as green
		// 	tree.DefaultMenuStyle.SetAlignTriangleLeft(true).SetTrianglePadding(1);
		// 	
		// 	OdinMenuItem odinMenuItem = tree.MenuItems.Last(x => x.Name == "BuildScenes");
		// 	OdinMenuStyle odinMenuStyle = OdinMenuStyle.TreeViewStyle;
		// 	
		// 	GUIStyle defaultLabelStyle = new GUIStyle();
		// 	defaultLabelStyle.richText = true;
		// 	
		// 	GUIStyle selectedLabelStyle = new GUIStyle();
		// 	selectedLabelStyle.richText = true;
		// 	
		// 	odinMenuItem.Name = "<color=#5c9949>BuildScenes</color>";
		//
		// 	odinMenuStyle.DefaultLabelStyle = defaultLabelStyle;
		// 	odinMenuStyle.SelectedLabelStyle = selectedLabelStyle;
		//
		// 	odinMenuItem.Style = odinMenuStyle;
		// 	
		// 	// Events
		// 	tree.Selection.SelectionConfirmed += OnSelectionConfirmed;
		//
		// 	return tree;
		// }

		// Disabling the drawing of selection value as right side ui
		protected override IEnumerable<object> GetTargets() {
			return Enumerable.Empty<object>();
		}
		protected override void DrawEditors() { }

		protected override void OnImGUI() {
			EditorGUILayout.BeginHorizontal();
			
			EditorGUI.BeginChangeCheck();
			ShowAllScenes = EditorGUILayout.Toggle("Show All Scenes", ShowAllScenes);
			if (EditorGUI.EndChangeCheck()) {
				Initialize();
			}

			if (GUILayout.Button("Configs")) {
				SceneConfigsWindow.CreateOrFocus(SceneConfigsWindow.SceneConfigAssetPath);
			}

			EditorGUILayout.EndHorizontal();
			
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2) {
				// OnSelectionConfirmed(MenuTree.Selection);
				Event.current.Use();
			}
			base.OnImGUI();
		}
	}
}
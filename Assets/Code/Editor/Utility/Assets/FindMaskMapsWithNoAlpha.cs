using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.VFX;

namespace Awaken.TG.Editor.Utility.Assets
{
	public class FindMaskMapsWithNoAlpha : EditorWindow {
		public string SearchPhrase = "t:texture2d MaskMap";
	
		List<Object> textures = new();
		Vector2 scrollPos;
		
		[MenuItem("TG/Assets/Find MaskMaps with no alpha...")]
		static void Init() {
			var window = (FindMaskMapsWithNoAlpha)GetWindow(typeof(FindMaskMapsWithNoAlpha));
			window.Show();
		}
		
		void OnGUI() {
			EditorGUILayout.LabelField("Search phrase:");
			SearchPhrase = EditorGUILayout.TextField(SearchPhrase);
			GUILayout.Space(25);
			if (GUILayout.Button("Find"))
				Find();

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			for (int i = 0; i < textures.Count; i++) {
				EditorGUILayout.ObjectField("Found texture:", textures[i], typeof(Object), false);
			}
			EditorGUILayout.EndScrollView();
		}

		void Find() {
			textures.Clear();
			var guids = AssetDatabase.FindAssets(SearchPhrase);
			var dbg = string.Empty;
			for (int i = 0; i < guids.Length; i++) {
				var guid = guids[i];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				Texture2D tex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
				if (tex == null)
					continue;
				if (GraphicsFormatUtility.HasAlphaChannel(tex.graphicsFormat) == false)
					textures.Add(tex);
			}

			Debug.Log($"<size=16><color=white>{dbg}</color></size>");
		}

		// void OnWizardOtherButton() {
		// 	foreach (var selectedGO in Selection.gameObjects) {
		// 		GameObject newObject;
		// 		newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
		// 		newObject.transform.parent = selectedGO.transform.parent;
		// 		newObject.transform.SetPositionAndRotation(selectedGO.transform.position, selectedGO.transform.rotation);
		// 		DestroyImmediate(selectedGO);
		// 	}
		// }
	}
}

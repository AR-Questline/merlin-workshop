using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Editor.Utility.Assets
{
	public class FindVFXWithProperty : EditorWindow {
		public string property = "KandraRenderer_Indices";
		List<Object> vfxes = new();
		Vector2 scrollPos;
		
		[MenuItem("TG/Assets/Find VFX With Property...")]
		static void Init() {
			var window = (FindVFXWithProperty)GetWindow(typeof(FindVFXWithProperty));
			window.Show();
		}
		
		void OnGUI() {
			property = EditorGUILayout.TextField(property);
			GUILayout.Space(25);
			if (GUILayout.Button("Find"))
				Find();

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			for (int i = 0; i < vfxes.Count; i++) {
				EditorGUILayout.ObjectField("Found VFX:", vfxes[i], typeof(Object), false);
			}
			EditorGUILayout.EndScrollView();
		}

		void Find() {
			var guids = AssetDatabase.FindAssets("t:visualeffectasset");
			var dbg = string.Empty;
			for (int i = 0; i < guids.Length; i++) {
				var guid = guids[i];
				var path = AssetDatabase.GUIDToAssetPath(guid);
				VisualEffectAsset vfx = AssetDatabase.LoadAssetAtPath(path, typeof(VisualEffectAsset)) as VisualEffectAsset;
				if (vfx == null)
					continue;
				var prop = new List<VFXExposedProperty>();
				vfx.GetExposedProperties(prop);

				for (int j = 0; j < prop.Count; j++) {
					if (prop[j].name == property) {
						dbg += vfx.name + "\n";
						vfxes.Add(vfx);
					}
				}
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

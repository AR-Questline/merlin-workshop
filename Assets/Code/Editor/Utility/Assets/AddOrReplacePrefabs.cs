using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Assets
{
	public class AddOrReplacePrefabs : ScriptableWizard
	{
		public GameObject prefab;

		[MenuItem("TG/Assets/Add prefab or replace selected...")]
		static void CreateWizard() {
			DisplayWizard("Add prefab as child or replace selected gameobjects with prefab", typeof(AddOrReplacePrefabs), "Add", "Replace");
		}

		void OnWizardCreate() {
			foreach (var selectedGO in Selection.gameObjects) {
				var newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
				newObject.transform.parent = selectedGO.transform;
				newObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			}
		}

		void OnWizardOtherButton() {
			foreach (var selectedGO in Selection.gameObjects) {
				GameObject newObject;
				newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
				newObject.transform.parent = selectedGO.transform.parent;
				newObject.transform.SetPositionAndRotation(selectedGO.transform.position, selectedGO.transform.rotation);
				DestroyImmediate(selectedGO);
			}
		}
	}
}

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace Awaken.TG.Editor.Design {
    public class CreateLocationPrefabs : EditorWindow {
        [MenuItem("ArtTools/Create Location Prefabs", priority = 10)]
        static void CreateCreateStaticAndDynamicSections() {
            EditorWindow.GetWindow<CreateLocationPrefabs>();
        }
        string locationName;
        string path = "Assets/Scenes/CampaignMap/LocationPrefabs";
        string tempPath;
        int currentTry = 0;
        private void OnGUI() {
            EditorGUILayout.LabelField("Location Name: ");
            locationName = EditorGUILayout.TextField(locationName);
            if (GUILayout.Button("Create Prefabs")) {
                currentTry = 0;
                tempPath = GetPath();

                Transform tempParent = CreateEmptyGameobject(locationName, null).transform;
                StageUtility.PlaceGameObjectInCurrentStage(tempParent.gameObject);
                
                CreatePrefab(CreateEmptyGameobject(locationName + "_StaticGeometry", tempParent));
                CreatePrefab(CreateEmptyGameobject(locationName + "_Lights", tempParent));
                CreatePrefab(CreateEmptyGameobject(locationName + "_NPCs", tempParent));
                CreatePrefab(CreateEmptyGameobject(locationName + "_Logic", tempParent));
                CreateEmptyGameobject(locationName + "_LevelArt", tempParent);
                
                CreatePrefab(tempParent.gameObject);
            }
    
            GUI.enabled = false;
        }
        string GetPath() {
            string temp = $"{path}/{locationName}/";
            while(Directory.Exists(temp)) {
                temp = $"{path}/{locationName}_{currentTry}"; 
                currentTry++;
            }
            Directory.CreateDirectory(temp);
            return temp;
        }
        
        GameObject CreateEmptyGameobject(string name, Transform parent) {
            GameObject go = new GameObject();
            go.name = name;
            go.transform.parent = parent;
            return go;
        }
        void CreatePrefab(GameObject go) {
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{tempPath}/{go.name}.prefab", InteractionMode.AutomatedAction);
        }
    }
}
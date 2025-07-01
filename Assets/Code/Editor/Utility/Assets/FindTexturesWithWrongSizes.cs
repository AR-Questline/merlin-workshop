using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Assets {
    public class FindTexturesWithWrongSizes : EditorWindow {
        [SerializeField] string _searchPhrase = "t:texture2d";
        [SerializeField] string[] _folders = {"Assets/2DAssets"};

        List<Texture2D> _textures = new();
        Vector2 _scrollPos;

        SerializedObject _serializedObject;
        SerializedProperty _searchPhraseProp;
        SerializedProperty _foldersProp;

        [MenuItem("TG/Assets/Find Textures with wrong sizes...")]
        static void Init() {
            var window = GetWindow(typeof(FindTexturesWithWrongSizes));
            window.Show();
        }

        void OnEnable() {
            _serializedObject = new SerializedObject(this);
            _searchPhraseProp = _serializedObject.FindProperty("_searchPhrase");
            _foldersProp = _serializedObject.FindProperty("_folders");
        }

        void OnGUI() {
            _serializedObject.Update();

            EditorGUILayout.PropertyField(_searchPhraseProp, false);
            EditorGUILayout.PropertyField(_foldersProp, true);

            GUILayout.Space(25);
            if (GUILayout.Button("Find")) {
                Find();
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _textures.Count; i++) {
                EditorGUILayout.ObjectField("Found texture:", _textures[i], typeof(Texture2D), false);
            }
            EditorGUILayout.EndScrollView();

            _serializedObject.ApplyModifiedProperties();
        }

        void Find() {
            _textures.Clear();
            var guids = AssetDatabase.FindAssets(_searchPhrase, _folders);
            //var dbg = string.Empty;
            for (int i = 0; i < guids.Length; i++) {
                var guid = guids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) {
                    continue;
                }

                importer.GetSourceTextureWidthAndHeight(out var width, out var height);
                if (width % 4 != 0 || height % 4 != 0) {
                    var tex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
                    if (tex != null) {
                        _textures.Add(tex);
                    }
                }
            }


            //Debug.Log($"<size=16><color=white>{dbg}</color></size>");

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
}

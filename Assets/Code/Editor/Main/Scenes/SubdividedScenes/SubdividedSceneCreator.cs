using Awaken.TG.Graphics.Culling;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Main.Scenes.SubdividedScenes {
    public class SubdividedSceneCreator : OdinEditorWindow {
        [SerializeField, Sirenix.OdinInspector.FilePath] string _saveFolder;
        [SerializeField] string fullName;
        [SerializeField] string friendlyName;
        SerializedProperty _serializedProperty;
        short _parentNodeIndex;
        public static void Show(SerializedProperty property, Scene motherScene, short parentNodeIndex) {
            var wizard = GetWindow<SubdividedSceneCreator>("Subscene Creator", true);
            wizard._serializedProperty = property;
            string motherScenePath = motherScene.path;
            wizard._saveFolder = $"{motherScenePath[..motherScene.path.LastIndexOf('.')]}/Subscenes";
            wizard.fullName = "";
            wizard.friendlyName = "";
            wizard._parentNodeIndex = parentNodeIndex;
        }

        [Button]
        void Create() {
            if (string.IsNullOrWhiteSpace(_saveFolder)) {
                Log.Important?.Error("You need to specify scene folder");
                return;
            }

            if (!_saveFolder.StartsWith("Assets/")) {
                Log.Important?.Error("You need to specify scene folder within Assets folder");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(fullName)) {
                Log.Important?.Error("You need to specify scene fullName");
                return;
            }

            string[] folders = _saveFolder.Split('/');
            var path = folders[0];
            for (int i = 1; i < folders.Length; i++) {
                var nextPath = $"{path}/{folders[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath)) {
                    var guid = AssetDatabase.CreateFolder(path, folders[i]);
                    if (string.IsNullOrEmpty(guid)) {
                        Log.Important?.Error("Specified path is invalid");
                        return;
                    }
                }
                path = nextPath;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObjects.Empty("Root", scene)
                .WithComponent<SubdividedSceneChild>()
                .WithComponent<DistanceCuller>();
            GameObjects.Empty("Loot", scene);
            GameObjects.Empty("Enemies", scene);
            GameObjects.Empty("Resources", scene);
            GameObjects.Empty("NPC", scene);
            
            var scenePath = $"{_saveFolder}/{fullName}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            SerializedSubscenesDataDrawer.AddScene(_parentNodeIndex, friendlyName, scenePath, _serializedProperty);
            Close();
        }
    }
}
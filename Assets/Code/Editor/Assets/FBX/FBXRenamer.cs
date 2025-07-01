using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Assets.FBX {
    public class FBXRenamer : OdinEditorWindow {
        [ShowInInspector] string _valueToSwap = "";
        [ShowInInspector] string _substituteValue = "";
        [ShowInInspector] bool _renameTransforms = true;
        [ShowInInspector] bool _renameMeshes = true;
        [ShowInInspector] List<GameObject> _fbxToRename = new();
        
        
        const int Width = 500;
        const int Height = 300;
        static FBXRenamer s_editor;
        static int s_x, s_y;

        [MenuItem("TG/Assets/Rename FBX", priority = 100)]
        static void ShowEditor() {
            s_editor = EditorWindow.GetWindow<FBXRenamer>();
            CenterWindow();
        }

        protected override void OnImGUI() {
            base.OnImGUI();
            if (GUILayout.Button("Rename")) {
                Rename();
            }
        }

        public void Rename() {
            try {
                AssetDatabase.StartAssetEditing(); //Prevents reimport for each changed/created file
                
                foreach (var go in _fbxToRename) {
                    go.name = go.name.Replace(_valueToSwap, _substituteValue);
                    
                    foreach (Transform transform in go.transform) {
                        if(_renameTransforms) {
                            transform.name = transform.name.Replace(_valueToSwap, _substituteValue);
                        }
                        
                        if (_renameMeshes && transform.TryGetComponent(out MeshFilter mf)) {
                            var mesh = mf.sharedMesh;
                            mesh.name = mesh.name.Replace(_valueToSwap, _substituteValue);
                        }
                    }
                
                    GameObject fbx = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
                    var path = AssetDatabase.GetAssetPath(fbx);


                    string destFile = path.Replace(Path.GetFileNameWithoutExtension(path), go.name);
                    Log.Important?.Info("From: " + (path) + " \nRenamed to: " + destFile);
                    // Cache the meta file so that fbx exporter doesn't override it
                    File.Move(path + ".meta", (destFile + ".meta.temp"));
                    ExportBinaryFBX(path,go);
                    // override the new meta with the old meta
                    File.Move(destFile + ".meta.temp", (path + ".meta"));
                    AssetDatabase.RenameAsset(path, go.name);
                    
                }
                _fbxToRename.Clear();
            } finally {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        static void CenterWindow() {
            s_editor = EditorWindow.GetWindow<FBXRenamer>();
            s_x = (Screen.currentResolution.width - Width) / 2;
            s_y = (Screen.currentResolution.height - Height) / 2;
            s_editor.position = new Rect(s_x, s_y, Width, Height);
            s_editor.titleContent = new GUIContent("FBX Renamer");
        }

        public static void ExportBinaryFBX(string filePath, UnityEngine.Object singleObject) {
#if UNITY_EDITOR
            // // Find relevant internal types in Unity.Formats.Fbx.Editor assembly
            // Type[] types = AppDomain.CurrentDomain.GetAssemblies().First(x =>
            //                             x.FullName ==
            //                             "Unity.Formats.Fbx.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
            //                         .GetTypes();
            // Type optionsInterfaceType = types.First(x => x.Name == "IExportOptions");
            // Type optionsType = types.First(x => x.Name == "ExportOptionsSettingsSerializeBase");

            // Instantiate a settings object instance
            // MethodInfo optionsProperty = typeof(ModelExporter)
            //                              .GetProperty("DefaultOptions", BindingFlags.Static | BindingFlags.NonPublic)
            //                              .GetGetMethod(true);
            // object optionsInstance = optionsProperty.Invoke(null, null);
            //
            // // Change the export setting from ASCII to binary
            // FieldInfo exportFormatField =
            //     optionsType.GetField("exportFormat", BindingFlags.Instance | BindingFlags.NonPublic);
            // exportFormatField.SetValue(optionsInstance, 1);
            //
            // // Invoke the ExportObject method with the settings param
            // MethodInfo exportObjectMethod = typeof(ModelExporter).GetMethod("ExportObject",
            //     BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder,
            //     new Type[] {typeof(string), typeof(UnityEngine.Object), optionsInterfaceType}, null);
            // exportObjectMethod.Invoke(null, new object[] {filePath, singleObject, optionsInstance});
#endif
        }
    }
}

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Graphics {
    public class BatchVertexColorPainter : OdinEditorWindow {
        [ColorUsage(false), Tooltip("Target color to paint the vertices."), PropertyOrder(0)]
        public Color targetColor = Color.black;

        [Button(size: ButtonSizes.Large), PropertyOrder(1)]
        [Tooltip("Add all meshes from selected FBX files to the list.")]
        void AddMeshesFromSelectedFBX() {
            meshList.Clear();
        
            Object[] selectedObjects = Selection.objects;

            foreach (Object obj in selectedObjects) {
                string path = AssetDatabase.GetAssetPath(assetObject: obj);
                if (path.EndsWith(".fbx", comparisonType: StringComparison.OrdinalIgnoreCase)) {
                    GameObject fbxObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath: path);

                    if (fbxObject != null) {
                        MeshFilter[] meshFilters = fbxObject.GetComponentsInChildren<MeshFilter>();
                        foreach (MeshFilter meshFilter in meshFilters) {
                            Mesh mesh = meshFilter.sharedMesh;
                            if (mesh != null && !meshList.Contains(item: mesh)) meshList.Add(item: mesh);
                        }

                        SkinnedMeshRenderer[] skinnedRenderers = fbxObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers) {
                            Mesh mesh = skinnedRenderer.sharedMesh;
                            if (mesh != null && !meshList.Contains(item: mesh)) meshList.Add(item: mesh);
                        }
                    }
                }
            }

            Debug.Log($"Added {meshList.Count} meshes from selected FBX files.");
        }

        [Button(size: ButtonSizes.Large), PropertyOrder(2)]
        [Tooltip("Paint all meshes in the list with the target color.")]
        void PaintAllMeshesInList() {
            if (meshList.Count == 0) {
                Debug.LogWarning("Mesh list is empty. Add some meshes first.");
                return;
            }

            foreach (Mesh mesh in meshList.Where(m => m != null)) {
                PaintMesh(mesh: mesh, color: targetColor);
            }

            Debug.Log($"Painted {meshList.Count} meshes with color: {targetColor}");
        }

        [ListDrawerSettings(DefaultExpandedState = true, DraggableItems = false, ShowPaging = false, NumberOfItemsPerPage = 10)]
        [Tooltip("List of meshes to paint."), PropertyOrder(3)]
        public List<Mesh> meshList = new();

        void PaintMesh(Mesh mesh, Color color) {
            if (mesh == null) {
                Debug.LogWarning("Mesh is null. Skipping painting.");
                return;
            }

            int vertexCount = mesh.vertexCount;
            Color[] colors = new Color[vertexCount];
            
            for (int i = 0; i < vertexCount; i++) {
                colors[i] = color;
            }
            mesh.colors = colors;
            
            string path = AssetDatabase.GetAssetPath(mesh);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) {
                EditorUtility.SetDirty(mesh);
                Debug.Log($"Updated mesh: {mesh.name} in FBX file: {path}");
                
                UpdateFBXWithMeshes(path, mesh);
            } else {
                EditorUtility.SetDirty(mesh);
                Debug.Log($"Updated mesh asset: {mesh.name}");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void UpdateFBXWithMeshes(string path, Mesh updatedMesh) {
            GameObject fbxObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath: path);
            if (fbxObject == null) {
                Debug.LogError($"Failed to load FBX at {path}");
                return;
            }
            
            MeshFilter[] meshFilters = fbxObject.GetComponentsInChildren<MeshFilter>();
            SkinnedMeshRenderer[] skinnedRenderers = fbxObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (MeshFilter meshFilter in meshFilters) {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh != null) {
                    mesh.colors = updatedMesh.colors;
                    EditorUtility.SetDirty(mesh);
                }
            }

            foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers) {
                Mesh mesh = skinnedRenderer.sharedMesh;
                if (mesh != null) {
                    mesh.colors = updatedMesh.colors;
                    EditorUtility.SetDirty(mesh);
                }
            }
            
            ExportFBX(path, fbxObject);
        }

        void ExportFBX(string path, GameObject fbxObject) {
            string fbxFilePath = Application.dataPath.Replace("Assets", "") + path;
            ExportBinaryFBX(fbxFilePath, fbxObject);
            Debug.Log($"Exported FBX with updated meshes to {path}");
        }

        [MenuItem("ArtTools/Vertex Color/Batch Vertex Color Painter")]
        static void OpenWindow() {
            GetWindow<BatchVertexColorPainter>().Show();
        }
        
        static void ExportBinaryFBX(string filePath, Object singleObject) {
#if UNITY_EDITOR
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().First(x =>
                    x.FullName == "Unity.Formats.Fbx.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                .GetTypes();
            Type optionsInterfaceType = types.First(x => x.Name == "IExportOptions");
            Type optionsType = types.First(x => x.Name == "ExportOptionsSettingsSerializeBase");

            // MethodInfo optionsProperty = typeof(ModelExporter)
            //     .GetProperty("DefaultOptions", BindingFlags.Static | BindingFlags.NonPublic)
            //     ?.GetGetMethod(true);
            // if (optionsProperty != null) {
            //     object optionsInstance = optionsProperty.Invoke(null, null);
            //
            //     FieldInfo exportFormatField =
            //         optionsType.GetField("exportFormat", BindingFlags.Instance | BindingFlags.NonPublic);
            //     if (exportFormatField != null) exportFormatField.SetValue(optionsInstance, 1);
            //
            //     MethodInfo exportObjectMethod = typeof(ModelExporter).GetMethod("ExportObject",
            //         BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder,
            //         new[] {typeof(string), typeof(Object), optionsInterfaceType}, null);
            //     if (exportObjectMethod != null)
            //         exportObjectMethod.Invoke(null, new[] { filePath, singleObject, optionsInstance });
            // }

#endif
        }
    }
}

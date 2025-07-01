using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics {
    public class TransferVertexColor : OdinEditorWindow {

        [HorizontalGroup("Split", 0.5f)]
        [BoxGroup("Split/Base Mesh"), LabelText("Base Mesh")]
        [ValidateInput("HasBaseMeshAssigned", "Base Mesh is not assigned.")]
        public GameObject baseMesh;  

        [HorizontalGroup("Split", 0.5f)]
        [BoxGroup("Split/Target Meshes"), LabelText("Target Meshes")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "name")]
        [ValidateInput("HasTargetMeshesAssigned", "No Target Meshes are assigned.")]
        public GameObject[] targetMeshes = Array.Empty<GameObject>();

        [MenuItem("ArtTools/Transfer Vertex Color")]
        public static void ShowWindow() {
            var window = GetWindow<TransferVertexColor>("Transfer Vertex Color");
            window.autoRepaintOnSceneChange = true;
        }

        [Button(ButtonSizes.Large), GUIColor(0.5f, 0.5f, 1.0f)]
        [EnableIf("CanTransferColors")]
        public void TransferVertexColors() {
            Mesh baseMeshFilter = baseMesh.GetComponent<MeshFilter>()?.sharedMesh;
            if (baseMeshFilter == null) {
                Debug.LogError("Base Mesh is not assigned.");
                return;
            }

            Color[] baseMeshColors = GetOrAssignDefaultColors(baseMeshFilter);
            Vector3[] baseMeshVertices = baseMeshFilter.vertices;

            foreach (GameObject targetMesh in targetMeshes) {
                ProcessTargetMesh(targetMesh, baseMeshColors, baseMeshVertices);
            }

            Repaint();
        }

        void ProcessTargetMesh(GameObject targetMesh, Color[] baseMeshColors, Vector3[] baseMeshVertices) {
            Mesh mesh = targetMesh.GetComponent<MeshFilter>()?.sharedMesh;

            if (mesh == null) {
                Debug.LogError($"Target mesh on {targetMesh.name} is not assigned.");
                return;
            }

            Vector3[] vertices = mesh.vertices;
            if (vertices.Length == 0) {
                Debug.LogError($"Mesh {targetMesh.name} has no vertices.");
                return;
            }

            Color[] colors = GetOrAssignDefaultColors(mesh, vertices.Length);

            for (int i = 0; i < vertices.Length; i++) {
                int nearestIndex = FindNearestVertex(vertices[i], baseMeshVertices);

                if (nearestIndex >= 0 && nearestIndex < baseMeshColors.Length) {
                    colors[i] = baseMeshColors[nearestIndex];
                } else {
                    Debug.LogWarning($"Vertex out of range for mesh {targetMesh.name} at vertex {i}.");
                }
            }
    
            mesh.colors = colors;
            EditorUtility.SetDirty(mesh);
            
            string assetPath = AssetDatabase.GetAssetPath(targetMesh);
            string exportPath = assetPath.Insert(assetPath.Length - 4, "_TEMP");
            ExportBinaryFBX(exportPath, targetMesh);

            File.Delete(assetPath);
            File.Move(exportPath, assetPath);
            
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Color[] GetOrAssignDefaultColors(Mesh mesh, int vertexCount = -1) {
            Color[] colors = mesh.colors;

            if (colors == null || colors.Length == 0) {
                Debug.LogWarning($"{mesh.name} does not have vertex colors. Assigning default black colors.");
                int count = vertexCount >= 0 ? vertexCount : mesh.vertices.Length;
                colors = Enumerable.Repeat(Color.black, count).ToArray();
            }

            return colors;
        }

        int FindNearestVertex(Vector3 targetVertex, Vector3[] vertices) {
            int nearestIndex = 0;
            float nearestDistanceSquare = Mathf.Infinity;

            for (int i = 0; i < vertices.Length; i++) {
                float distSquare = (targetVertex - vertices[i]).sqrMagnitude;
                if (distSquare < nearestDistanceSquare) {
                    nearestIndex = i;
                    nearestDistanceSquare = distSquare;
                }
            }

            return nearestIndex;
        }

        static void ExportBinaryFBX(string filePath, UnityEngine.Object singleObject) {
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
            //         new[] {typeof(string), typeof(UnityEngine.Object), optionsInterfaceType}, null);
            //     if (exportObjectMethod != null)
            //         exportObjectMethod.Invoke(null, new[] { filePath, singleObject, optionsInstance });
            // }

#endif
        }

        bool HasBaseMeshAssigned(GameObject baseMesh) {
            return baseMesh != null;
        }

        bool HasTargetMeshesAssigned(GameObject[] targetMeshes) {
            return targetMeshes is { Length: > 0 };
        }

        bool CanTransferColors() {
            return HasBaseMeshAssigned(baseMesh) && HasTargetMeshesAssigned(targetMeshes);
        }
    }
}

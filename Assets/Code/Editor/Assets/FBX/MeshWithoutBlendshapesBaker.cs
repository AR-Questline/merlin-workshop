using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets.FBX {
    public class MeshWithoutBlendshapesBaker : OdinEditorWindow {
        [SerializeField, FolderPath(RequireExistingPath = true)] string directory;
        [SerializeField] string fileName;
        [SerializeField] string postfix;
        [SerializeField] Mesh mesh;

        float[] _weights;
        Vector2 _scroll;
        
        protected override void OnImGUI() {
            base.OnImGUI();
            if (!mesh) {
                return;
            }
            int blendShapeCount = mesh.blendShapeCount;
            if (_weights == null) {
                _weights = new float[blendShapeCount];
            } else if (blendShapeCount != _weights.Length) {
                Array.Resize(ref _weights, blendShapeCount);
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < blendShapeCount; i++) {
                _weights[i] = EditorGUILayout.Slider(mesh.GetBlendShapeName(i), _weights[i], 0, 100);
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Bake")) {
                Bake();
            }
            if (GUILayout.Button("Bake Each Maxed")) {
                BakeEachMaxed();
            }
            GUILayout.FlexibleSpace();
        }

        [MenuItem("TG/Assets/Mesh/No Blendshapes Baker")]
        static void ShowEditor() {
            EditorWindow.GetWindow<MeshWithoutBlendshapesBaker>().Show();
        }

        void Bake() {
            var modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(this.mesh)) as ModelImporter;
            bool needsReadableChange = !modelImporter.isReadable;
            
            if (needsReadableChange) {
                modelImporter.isReadable = true;
                modelImporter.SaveAndReimport();
            }
            
            var mesh = Object.Instantiate(this.mesh);
            
            if (needsReadableChange) {
                modelImporter.isReadable = false;
                modelImporter.SaveAndReimport();
            }
            
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            
            int vertexCount = vertices.Length;
            int blendShapeCount = mesh.blendShapeCount;
            var deltaVertices = new Vector3[vertexCount];
            var deltaNormals = new Vector3[vertexCount];
            var deltaTangents = new Vector3[vertexCount];
            var deltaVerticesHelp = new Vector3[vertexCount];
            var deltaNormalsHelp = new Vector3[vertexCount];
            var deltaTangentsHelp = new Vector3[vertexCount];

            for (int shape = 0; shape < blendShapeCount; shape++) {
                if (_weights[shape] > 0) {
                    CalculateDeltas(mesh, shape, _weights[shape], deltaVertices, deltaNormals, deltaTangents, deltaVerticesHelp, deltaNormalsHelp, deltaTangentsHelp);
                    for (int i = 0; i < vertexCount; i++) {
                        vertices[i] += deltaVertices[i];
                        normals[i] += deltaNormals[i];
                        tangents[i] += new Vector4(deltaTangents[i].x, deltaTangents[i].y, deltaTangents[i].z, 0);
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.ClearBlendShapes();
            mesh.RecalculateTangents(~MeshUpdateFlags.Default);
            
            var path = string.IsNullOrWhiteSpace(postfix) 
                ? $"{directory}/{fileName}.mesh" 
                : $"{directory}/{fileName}_{postfix}.mesh";
            AssetDatabase.CreateAsset(mesh, path);
        }

        void BakeEachMaxed() {
            int blendShapeCount = mesh.blendShapeCount;
            if (_weights == null) {
                _weights = new float[blendShapeCount];
            } else if (blendShapeCount != _weights.Length) {
                Array.Resize(ref _weights, blendShapeCount);
            }
            Array.Fill(_weights, 0);
            postfix = "Base";
            Bake();
            for (int i = 0; i < blendShapeCount; i++) {
                _weights[i] = 100;
                postfix = mesh.GetBlendShapeName(i);
                Bake();
                _weights[i] = 0;
            }
        }

        /// <param name="vertices">are filled with vertices deltas of given blendshape. Must be of length of mesh.vertexCount.</param>
        /// <param name="normals">are filled with normals deltas of given blendshape. Must be of length of mesh.vertexCount.</param>
        /// <param name="verticesHelp">temporary array passed here to lower allocation count. Must be of length of mesh.vertexCount.</param>
        /// <param name="normalsHelp">temporary array passed here to lower allocation count. Must be of length of mesh.vertexCount.</param>
        void CalculateDeltas(Mesh mesh, int blendshape, float weight, Vector3[] vertices, Vector3[] normals, Vector3[] tangents, Vector3[] verticesHelp, Vector3[] normalsHelp, Vector3[] tangentsHelp) {
            int vertexCount = vertices.Length;
            if (weight == 0) {
                for (int i = 0; i < vertexCount; i++) {
                    vertices[i] = Vector3.zero;
                    normals[i] = Vector3.zero;
                }
                return;
            }
            
            int frameCount = mesh.GetBlendShapeFrameCount(blendshape);
            float previousWeight = 0;
            for (int frame = 0; frame < frameCount; frame++) {
                float currentWeight = mesh.GetBlendShapeFrameWeight(blendshape, frame);
                
                if (currentWeight == weight) {
                    mesh.GetBlendShapeFrameVertices(blendshape, frame, vertices, normals, tangents);
                    return;
                }
                
                if (currentWeight > weight) {
                    if (frame == 0) {
                        mesh.GetBlendShapeFrameVertices(blendshape, frame, vertices, normals, tangents);
                        float factor = weight / currentWeight;
                        for (int i = 0; i < vertexCount; i++) {
                            vertices[i] *= factor;
                            normals[i] *= factor;
                        }
                    } else {
                        mesh.GetBlendShapeFrameVertices(blendshape, frame - 1, verticesHelp, normalsHelp, tangentsHelp);
                        mesh.GetBlendShapeFrameVertices(blendshape, frame, vertices, normals, tangents);
                        float factor = (weight - previousWeight) / (currentWeight - previousWeight);
                        for (int i = 0; i < vertexCount; i++) {
                            vertices[i] = Vector3.Lerp(verticesHelp[i], vertices[i], factor);
                            normals[i] = Vector3.Lerp(normalsHelp[i], normals[i], factor);
                            tangents[i] = Vector3.Lerp(tangentsHelp[i], tangents[i], factor);
                        }
                    }
                    return;
                }
                
                previousWeight = currentWeight;
            }
        }
    }
}
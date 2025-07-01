using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces.Assets;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.Mathematics;
#if UNITY_EDITOR
using Awaken.Utility.GameObjects;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Splines;

namespace Awaken.TG.Graphics.ProceduralMeshes {
    [RequireComponent(typeof(SplineContainer))]
    [ExecuteInEditMode]
    [DisableIf("@UnityEngine.Application.isPlaying")]
    public class SplineMeshGenerator : MonoBehaviour, IEditorOnlyMonoBehaviour {
#if UNITY_EDITOR
        public Mesh mesh;
        
        #region Inspector Variables
        [NonSerialized]
        public Action OnMeshGenerated = delegate { };
        
        [FoldoutGroup("Edit", expanded: true), OnValueChanged(nameof(Reset))]
        [SerializeField] SplineContainer splineContainer;
        
        [FoldoutGroup("Edit", expanded: true)]
        [SerializeField] MeshFilter meshFilter;
        
        [FoldoutGroup("Edit", expanded: true), OnValueChanged(nameof(GenerateMesh))]
        public MeshGenerationMode generationMode = MeshGenerationMode.Horizontal;
        
        [FoldoutGroup("Edit", expanded: true), OnValueChanged(nameof(GenerateMesh))]
        [SerializeField, Range(2, 512)] int resolution = 16;
        
        [FoldoutGroup("Edit", expanded: true), OnValueChanged(nameof(GenerateMesh))]
        [SerializeField, Range(1, 256)] float halfWidth = 2f;

        [ShowInInspector, FoldoutGroup("Debug", expanded: true)]
        bool _showVertices;
        
        [FoldoutGroup("Debug", expanded: true)]
        [ShowInInspector] bool _showVerticesNumbers;
        
        [FoldoutGroup("Debug", expanded: true)]
        [ShowInInspector, Range(.1f, 4f)] float _verticesSize = .5f;
        
        [FoldoutGroup("Bake", expanded: true)]
        [SerializeField, Sirenix.OdinInspector.FilePath] string path = "Assets/Scenes/GeneratedMesh.asset";
        
        #endregion
        #region Private Variables
        int _splineIndex;
        float3 _position;
        float3 _forward;
        float3 _upVector;
        List<Vector3> _pointsPosition0 = new();
        List<Vector3> _pointsPosition1 = new();
        List<Vector3> _pointsPosition2 = new();
        Matrix4x4 _worldToLocal;
        
        #endregion
        #region Unity Callbacks

        void Awake() {
            if (Application.isPlaying) return;
            splineContainer = GetComponent<SplineContainer>();
            meshFilter = GetComponent<MeshFilter>();
            mesh = meshFilter.sharedMesh;
            if (mesh == null) {
                mesh = new Mesh();
                meshFilter.sharedMesh = mesh;
            }
        }

        void Reset() {
            gameObject.GetOrAddComponent<MeshFilter>();
            gameObject.GetOrAddComponent<MeshRenderer>();
            Awake();
            GenerateMesh();
        }

        void OnEnable() {
            if (Application.isPlaying) return;
            Spline.Changed += OnSplineChanged;
        }

        void OnDisable() {
            if (Application.isPlaying) return;
            Spline.Changed -= OnSplineChanged;
        }
        
        void OnDrawGizmos() {
            Transform _transform = transform;
            if (_showVertices) {
                foreach (Vector3 t in _pointsPosition0)
                    Gizmos.DrawSphere(_transform.TransformPoint(t), _verticesSize);

                Gizmos.color = Color.red;
                foreach (Vector3 t in _pointsPosition1)
                    Gizmos.DrawSphere(_transform.TransformPoint(t), _verticesSize);

                if (generationMode == MeshGenerationMode.Horizontal) {
                    Gizmos.color = Color.green;
                    foreach (Vector3 t in _pointsPosition2)
                        Gizmos.DrawSphere(_transform.TransformPoint(t), _verticesSize);
                }
            }

            if (_showVerticesNumbers) {
                for (int i = 0; i < mesh.vertices.Length; i++) {
                    Vector3 worldPosition = _transform.TransformPoint(mesh.vertices[i]);
                    Handles.Label(worldPosition, i.ToString());
                }
            }
        }
        #endregion

        #region Mesh Generation
        void GenerateMesh() {
            GetPoints();

            if (mesh == null) {
                mesh = meshFilter.sharedMesh;
                if (mesh == null) {
                    mesh = new Mesh();
                    meshFilter.sharedMesh = mesh;
                }
            } else {
                mesh.Clear();
            }

            int numberVertices = _pointsPosition1.Count * 2;
            
            GenerateVerticesAndUVs(numberVertices);
            GenerateTriangles(numberVertices);
            GenerateNormals(numberVertices);
            ApplyMesh();
        }
        
        void GenerateVerticesAndUVs(int numberVertices) {
            Vector3[] vertices = new Vector3[numberVertices];
            Vector2[] uv = new Vector2[numberVertices];

            // Calculate total arc length
            float totalArcLength = 0f;
            for (int i = 1; i < _pointsPosition1.Count; i++) {
                totalArcLength += Vector3.Distance(_pointsPosition1[i - 1], _pointsPosition1[i]);
            }

            // Generate UV based on arc length
            float currentArcLength = 0f;
            for (int i = 0; i < _pointsPosition1.Count; i++) {
                float t = currentArcLength / totalArcLength;

                vertices[i * 2] = _pointsPosition1[i];
                vertices[i * 2 + 1] = _pointsPosition2[i];

                uv[i * 2] = new Vector2(t, 0f);
                uv[i * 2 + 1] = new Vector2(t, 1f);

                if (i < _pointsPosition1.Count - 1) {
                    currentArcLength += Vector3.Distance(_pointsPosition1[i], _pointsPosition1[i + 1]);
                }
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
        }

        void GenerateTriangles(int numberVertices) {
            bool isClosed = splineContainer.Splines is {Count: > 0} && splineContainer.Splines[0].Closed;
            
            int numberOfSegments = _pointsPosition1.Count - 1;
            int numberOfTriangles = numberOfSegments * 2 + (isClosed ? 2 : 0);
            int[] triangles = new int[numberOfTriangles * 3]; 

            int triangleIndex = 0;
            for (int i = 0; i < numberOfSegments; i++) {
                int vertexIndex = i * 2;

                triangles[triangleIndex++] = vertexIndex;
                triangles[triangleIndex++] = vertexIndex + 2;
                triangles[triangleIndex++] = vertexIndex + 1;

                triangles[triangleIndex++] = vertexIndex + 2;
                triangles[triangleIndex++] = vertexIndex + 3;
                triangles[triangleIndex++] = vertexIndex + 1;
            }

            if (isClosed) {
                int lastVertexIndex = numberVertices - 1;

                triangles[triangleIndex++] = lastVertexIndex - 1;
                triangles[triangleIndex++] = 0;
                triangles[triangleIndex++] = lastVertexIndex;

                triangles[triangleIndex++] = 0;
                triangles[triangleIndex++] = 1;
                triangles[triangleIndex] = lastVertexIndex;
            }

            mesh.triangles = triangles;
        }

        void GenerateNormals(int numberVertices) {
            Vector3[] normals = new Vector3[numberVertices];
            Array.Fill(normals, Vector3.down);
            
            mesh.normals = normals;
            mesh.RecalculateNormals();
        }
        
        void ApplyMesh() {
            if (meshFilter.sharedMesh != mesh) {
                meshFilter.sharedMesh = mesh;
            }
            mesh.UploadMeshData(true);
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
            OnMeshGenerated?.Invoke();
        }
        
        #endregion
        #region Utility Methods
        void OnSplineChanged(Spline spline, int index, SplineModification splineModification) {
            if (spline != splineContainer.Spline) return;
            
            GenerateMesh();
        }
        
        void GetPoints() {
            _worldToLocal = transform.worldToLocalMatrix;
            _pointsPosition0.Clear();
            _pointsPosition1.Clear();
            _pointsPosition2.Clear();
            
            if (splineContainer == null || splineContainer.Splines == null || splineContainer.Splines.Count == 0) {
                Log.Important?.Warning("Spline Container doesn't contain any splines.");
                return;
            }
            
            bool isClosed = splineContainer.Splines[0].Closed;

            float step = 1f / resolution;
            int baseVerts = isClosed ? resolution - 1 : resolution;
            for (int i = 0; i <= baseVerts; i++) {
                float t = step * i;
                SampleSplineWidth(t, out Vector3 p0, out Vector3 p1, out Vector3 p2);
                _pointsPosition0.Add(p0);
                _pointsPosition1.Add(p1);
                _pointsPosition2.Add(p2);
            }
        }
        
        public Vector3[] GetPointsOnSpline() {
            if (_pointsPosition0.Count == 0) {
                GetPoints();
            }
            return _pointsPosition0.ToArray();
        }
        
        void SampleSplineWidth(float t, out Vector3 p0, out Vector3 p1, out Vector3 p2) {
            splineContainer.Evaluate(_splineIndex, t, out _position, out _forward, out _upVector);
            
            if (generationMode == MeshGenerationMode.Horizontal) {
                float3 right = Vector3.Cross(_forward, _upVector).normalized;
                p0 = _worldToLocal.MultiplyPoint3x4(_position);
                p1 = _worldToLocal.MultiplyPoint3x4(_position + (right * halfWidth));
                p2 = _worldToLocal.MultiplyPoint3x4(_position + (-right * halfWidth));
            } else {
                p0 = _worldToLocal.MultiplyPoint3x4(_position);
                p1 = _worldToLocal.MultiplyPoint3x4(_position + (math.up() * halfWidth * 2f));
                p2 = p0;
            }
        }
        
        [FoldoutGroup("Edit", expanded: true)]
        [Button("Reset Spline")]
        public void ResetSpline() {
            var container = Selection.activeGameObject.GetComponent<SplineContainer>();
            container.Splines = container.Splines.Select(spline => {
                for (int i = 0, c = spline.Count; i < c; i++) {
                    var knot = spline[i];
                    knot.Position.y = 0f;
                    knot.Rotation.value = float4.zero;
                    spline[i] = knot;
                }
 
                return spline;
            }).ToList();
            GenerateMesh();
        }
        
        [FoldoutGroup("Bake", expanded: true), Button("Bake")]
        void BakeMeshToFile() {
            if (meshFilter == null || mesh == null) {
                Log.Important?.Warning("MeshFilter or Mesh is missing. Unable to bake mesh.");
                return;
            }

            if (AssetDatabase.GetAssetPath(mesh) == path) {
                AssetDatabase.SaveAssetIfDirty(mesh);
                OnMeshGenerated?.Invoke();
                return;
            }
            
            Mesh bakedMesh = Instantiate(mesh);
            
            AssetDatabase.CreateAsset(bakedMesh, path);
            AssetDatabase.SaveAssets();
            meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh = meshFilter.sharedMesh;
            Log.Important?.Info("Mesh saved: " + path);
            OnMeshGenerated?.Invoke();
        }
        
        public enum MeshGenerationMode {
            Horizontal,
            Vertical
        }
        #endregion
#endif
    }
}
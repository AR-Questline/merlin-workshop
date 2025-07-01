using Awaken.Kandra.Managers;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector.Editor;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(KandraMesh))]
    public class KandraMeshEditor : OdinEditor {
        const int SubmeshTextureResolution = 32;
        static readonly int SliderHash = "KandraMeshSlider".GetHashCode();
        static readonly Color[] SubMeshColors = new Color[] {
            new Color(0.79f, 0.71f, 0.72f),
            new Color(0.22f, 0.22f, 0.22f),
            new Color(0.91f, 0.14f, 0.14f),
            new Color(0.14f, 0.81f, 0.14f),
            new Color(0.14f, 0.14f, 0.91f),
            new Color(0.95f, 0.95f, 0.11f),
            new Color(0.91f, 0.14f, 0.91f),
        };

        // == Inspector
        ulong _meshDataSize;
        ulong _indicesDataSize;

        bool _debugExpanded;
        bool _boneWeightsExpanded;

        // == Preview
        PreviewRenderUtility _previewUtility;
        Material[] _previewMaterials;
        Texture2D[] _previewTextures;
        Mesh _previewMesh;

        float _distanceModifier = 12f;
        Vector2 _previewDir = new Vector2(0, -20);

        // === Lifetime
        protected override unsafe void OnEnable() {
            base.OnEnable();
            InitPreview();

            var kandraMesh = (KandraMesh)target;

            var meshDataPath = KandraRendererManager.Instance.StreamingManager.MeshDataPath(kandraMesh);
            var fileInfo = default(FileInfoResult);
            AsyncReadManager.GetFileInfo(meshDataPath, &fileInfo).JobHandle.Complete();
            _meshDataSize = (ulong)fileInfo.FileSize;

            var indicesData = KandraRendererManager.Instance.StreamingManager.IndicesDataPath(kandraMesh);
            AsyncReadManager.GetFileInfo(indicesData, &fileInfo).JobHandle.Complete();
            _indicesDataSize = (ulong)fileInfo.FileSize;
        }

        protected override void OnDisable() {
            base.OnDisable();
            DisposePreview();
        }

        // === Public
        public void SetMaterials(Material[] materials) {
            for (int i = 0; i < materials.Length; i++) {
                if (_previewMaterials.Length <= i) {
                    break;
                }
                if (_previewMaterials[i] != null) {
                    DestroyImmediate(_previewMaterials[i]);
                }
                _previewMaterials[i] = new Material(materials[i]);
            }
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            GUILayout.Label($"Mesh data size: {M.HumanReadableBytes(_meshDataSize)}");
            GUILayout.Label($"Indices data size: {M.HumanReadableBytes(_indicesDataSize)}");

            _debugExpanded = EditorGUILayout.Foldout(_debugExpanded, "Debug");
            if (!_debugExpanded) {
                return;
            }

            var mesh = (KandraMesh)target;
            var meshData = mesh.ReadSerializedData(KandraRendererManager.Instance.StreamingManager.LoadMeshData(mesh));

            _boneWeightsExpanded = EditorGUILayout.Foldout(_boneWeightsExpanded, "Bone Weights");
            if (_boneWeightsExpanded) {
                var boneWeights = meshData.boneWeights;
                for (var i = 0u; i < boneWeights.Length; i++) {
                    var boneWeight = boneWeights[i];
                    EditorGUILayout.LabelField($"Bone {i}", $"{boneWeight.Index0} = {boneWeight.Weight0:P1}; {boneWeight.Index1} = {boneWeight.Weight1:P1}; {boneWeight.Index2} = {boneWeight.Weight2:P1}; {boneWeight.Index3} = {boneWeight.Weight3:P1}");
                }
            }
        }

        public override bool HasPreviewGUI() => true;

        public override void OnPreviewGUI(Rect r, GUIStyle background) {
            _previewDir = Drag2D(_previewDir, r);
            _previewDir.y = Mathf.Clamp(_previewDir.y, -120.0f, 120.0f);

            if (Event.current.type == EventType.Repaint) {
                _previewUtility.BeginPreview(r, background);

                DoRenderPreview((KandraMesh)target);

                _previewUtility.EndAndDrawPreview(r);
            }
        }

        public override void OnPreviewSettings() {
            base.OnPreviewSettings();

            var mesh = (KandraMesh)target;
            var textureBoxOptions = new GUILayoutOption[2] {
                GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(EditorGUIUtility.singleLineHeight),
            };
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField("Submeshes:", GUILayout.ExpandWidth(false));
            for (var i = 0; i < _previewTextures.Length; i++) {
                GUILayout.Box(_previewTextures[i], textureBoxOptions);
            }
            EditorGUILayout.EndHorizontal();
        }

        // === Private
        unsafe void InitPreview() {
            _previewUtility = new PreviewRenderUtility();

            var defaultMaterial = new Material(Shader.Find("HDRP/Lit"));
            _previewMaterials = new Material[SubMeshColors.Length];
            _previewTextures = new Texture2D[SubMeshColors.Length];
            var pixels = new Color[SubmeshTextureResolution * SubmeshTextureResolution];
            for (var i = 0; i < SubMeshColors.Length; i++) {
                _previewMaterials[i] = new Material(defaultMaterial);
                _previewMaterials[i].SetColor("_BaseColor", SubMeshColors[i]);
                //_previewMaterials[i].SetColor("_EmissiveColor", SubMeshColors[i]);

                _previewTextures[i] = new Texture2D(SubmeshTextureResolution, SubmeshTextureResolution);
                for (var j = 0; j < SubmeshTextureResolution*SubmeshTextureResolution; j++) {
                    pixels[j] = SubMeshColors[i];
                }
                _previewTextures[i].SetPixels(pixels);
                _previewTextures[i].Apply();
            }
            DestroyImmediate(defaultMaterial);

            var kandraMesh = (KandraMesh)target;
            var meshData = kandraMesh.ReadSerializedData(KandraRendererManager.Instance.StreamingManager.LoadMeshData(kandraMesh));

            var indicesCount = kandraMesh.indicesCount;

            var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(kandraMesh);
            var dataArray = Mesh.AllocateWritableMeshData(1);
            var data = dataArray[0];
            data.SetVertexBufferParams(kandraMesh.vertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));
            data.SetIndexBufferParams((int)indicesCount, IndexFormat.UInt16);
            var meshIndices = data.GetIndexData<ushort>();
            UnsafeUtility.MemCpy(meshIndices.GetUnsafePtr(), indices.Ptr, indicesCount * sizeof(ushort));

            var meshVerticesData = data.GetVertexData<VerticesData>();
            for (var i = 0u; i < kandraMesh.vertexCount; i++) {
                var vertex = meshData.vertices[i];
                var tangent = vertex.Tangent;
                var additionalData = meshData.additionalData[i];
                meshVerticesData[(int)i] = new VerticesData {
                    position = vertex.position,
                    normal = vertex.Normal,
                    tangent = new Vector4(tangent.x, tangent.y, tangent.z, additionalData.tangentW),
                    uv = additionalData.UV
                };
            }

            data.subMeshCount = kandraMesh.submeshes.Length;
            for (int i = 0; i < kandraMesh.submeshes.Length; i++) {
                var submesh = kandraMesh.submeshes[i];
                data.SetSubMesh(i, new SubMeshDescriptor((int)submesh.indexStart, (int)submesh.indexCount));
            }

            var meshName = "Original";
#if UNITY_EDITOR
            meshName = kandraMesh.name + "_Original";
#endif
            _previewMesh = new Mesh {
                name = meshName,
            };
            Mesh.ApplyAndDisposeWritableMeshData(dataArray, _previewMesh);
            _previewMesh.RecalculateBounds();
            _previewMesh.UploadMeshData(true);
        }

        void DisposePreview() {
            _previewUtility.Cleanup();
            for (int i = 0; i < _previewMaterials.Length; i++) {
                DestroyImmediate(_previewMaterials[i]);
            }
            DestroyImmediate(_previewMesh);
        }
        void DoRenderPreview(KandraMesh mesh) {
            //var bounds = mesh.meshLocalBounds;
            var bounds = _previewMesh.bounds;

            var halfSize = bounds.extents.magnitude;
            var distance = halfSize * _distanceModifier;

            var viewDir = -(_previewDir / 100.0f);

            _previewUtility.camera.transform.position = bounds.center +
                                                        (new Vector3(Mathf.Sin(viewDir.x) * Mathf.Cos(viewDir.y),
                                                             Mathf.Sin(viewDir.y),
                                                             Mathf.Cos(viewDir.x) * Mathf.Cos(viewDir.y)) *
                                                         distance);

            _previewUtility.camera.transform.LookAt(bounds.center);
            _previewUtility.camera.nearClipPlane = 0.05f;
            _previewUtility.camera.farClipPlane = 1000.0f;

            _previewUtility.lights[0].intensity = 1.0f;
            _previewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
            _previewUtility.lights[1].intensity = 1.0f;
            _previewUtility.ambientColor = new Color(.2f, .2f, .2f, 0);

            var matrix = Matrix4x4.TRS(Vector3.up * (bounds.extents.y * 0.25f), Quaternion.identity, Vector3.one);
            var submeshes = mesh.submeshes.Length;
            var wasWireframe = GL.wireframe;
            GL.wireframe = true;
            for (var i = 0; i < submeshes; i++) {
                _previewUtility.DrawMesh(_previewMesh, matrix, _previewMaterials[i % _previewMaterials.Length], i);
            }
            GL.wireframe = wasWireframe;

            _previewUtility.Render(Unsupported.useScriptableRenderPipeline);
        }

        Vector2 Drag2D(Vector2 scrollPosition, Rect position) {
            int controlId = GUIUtility.GetControlID(SliderHash, FocusType.Passive);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlId)) {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && position.width > 50.0) {
                        GUIUtility.hotControl = controlId;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                        GUIUtility.hotControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId) {
                        scrollPosition -= current.delta *
                                          (current.shift ? 3f : 1f) /
                                          Mathf.Min(position.width, position.height) *
                                          140f;
                        current.Use();
                        GUI.changed = true;
                    }

                    break;

                case EventType.ScrollWheel:
                    if (position.Contains(current.mousePosition) && position.width > 50.0) {
                        var speed = 0.1f;
                        if (current.shift) {
                            speed *= 2f;
                        } else if (current.control) {
                            speed *= 0.3f;
                        }

                        _distanceModifier += current.delta.y * speed;
                        current.Use();
                        GUI.changed = true;
                    }

                    break;
            }

            return scrollPosition;
        }

        struct VerticesData {
            public Vector3 position;
            public Vector3 normal;
            public Vector4 tangent;
            public Vector2 uv;
        }
    }
}

using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Awaken.TG.Utility;

namespace Awaken.Kandra.Editor {
    public class MeshResearch : EditorWindow {
        Mesh _mesh;
        GUIContent _currentMeshInfo = new GUIContent("No mesh selected");
        GUIContent _currentBlendshapeInfo = new GUIContent("No mesh selected");
        GUIStyle _labelStyle;
        Vector2 _scrollPosition;

        Vector3[] _verticesDelta;
        Vector3[] _normalsDelta;
        Vector3[] _tangentsDelta;

        void OnEnable() {
            _labelStyle = new GUIStyle(EditorStyles.label) {
                richText = true,
                wordWrap = true,
            };
        }

        void OnGUI() {
            var newMesh = EditorGUILayout.ObjectField("Mesh", _mesh, typeof(Mesh), false) as Mesh;
            if (newMesh != _mesh) {
                _mesh = newMesh;
                if (_mesh != null) {
                    CollectMeshInfo();
                } else {
                    _currentMeshInfo.text = "No mesh selected";
                    _currentBlendshapeInfo.text = string.Empty;
                }
            }

            var width = position.width;
            var height = _labelStyle.CalcHeight(_currentMeshInfo, width);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.LabelField(_currentMeshInfo, _labelStyle, GUILayout.MinHeight(height));

            EditorGUILayout.EndScrollView();
        }

        unsafe void CollectMeshInfo() {
            var sb = new StringBuilder();
            sb.Append("<b><i>Mesh: ");
            sb.Append(_mesh.name);
            sb.AppendLine("</i></b>");

            var vertexCount = (uint)_mesh.vertexCount;
            sb.Append("<b>Vertices:</b> ");
            sb.Append(vertexCount);
            sb.Append(" * ");

            var vertexStreamsCount = _mesh.vertexBufferCount;
            var vertexSize = 0u;
            for (var i = 0; i < vertexStreamsCount; i++) {
                vertexSize += (uint)_mesh.GetVertexBufferStride(i);
            }

            sb.Append(vertexSize);
            sb.Append(" [bytes] = ");

            var verticesSize = vertexCount * vertexSize;

            sb.Append(M.HumanReadableBytes(verticesSize));
            sb.AppendLine();

            var bonesCount = (uint)_mesh.bindposeCount;
            var attributesCount = _mesh.vertexAttributeCount;
            for (var i = 0; i < attributesCount; i++) {
                var attribute = _mesh.GetVertexAttribute(i);
                sb.Append("   <b>Attribute ");
                sb.Append(attribute.attribute.ToString());
                sb.Append(":</b> ");
                sb.Append(attribute.format.ToString());
                sb.Append("x");
                sb.Append(attribute.dimension);
                sb.Append(" = ");
                sb.Append(M.HumanReadableBytes(attribute.dimension * FormatSize(attribute.format)));
                sb.Append(" * ");
                var attributeCount = AttributeCount(attribute.attribute, vertexCount, bonesCount);
                sb.Append(attributeCount);
                sb.Append(" = ");
                sb.Append(M.HumanReadableBytes(attributeCount * attribute.dimension * FormatSize(attribute.format)));
                sb.Append(" in stream: ");
                sb.Append(attribute.stream);
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.Append("<b>Indices:</b> ");
            var submeshesCount = _mesh.subMeshCount;
            var indexCount = 0u;
            for (var i = 0; i < submeshesCount; i++) {
                indexCount += _mesh.GetIndexCount(i);
            }

            sb.Append(indexCount);
            sb.Append(" * ");

            var indexSize = _mesh.indexFormat == IndexFormat.UInt16 ? sizeof(ushort) : sizeof(uint);
            sb.Append(indexSize);
            sb.Append(" [bytes] = ");

            var indicesSize = indexCount * indexSize;
            sb.Append(M.HumanReadableBytes(indicesSize));
            sb.Append(" in ");
            sb.Append(submeshesCount);
            sb.AppendLine(" submeshes");
            sb.AppendLine();

            sb.Append("UV distribution: ");
            sb.Append(_mesh.GetUVDistributionMetric(0));
            sb.AppendLine();

            var bonesSize = 0u;
            if (bonesCount > 0) {
                sb.Append("<b>Bones:</b> ");
                sb.Append(bonesCount);
                sb.Append(" * ");
                sb.Append(sizeof(Matrix4x4));
                sb.Append(" [bytes] = ");
                bonesSize = bonesCount * (uint)sizeof(Matrix4x4);
                sb.Append(M.HumanReadableBytes(bonesSize));
                sb.AppendLine();
                sb.AppendLine();
            }

            var boneWeights = _mesh.boneWeights;
            if (boneWeights.Length > 0) {
                sb.Append("<b>Bone weights:</b> ");
                sb.Append(boneWeights.Length);
                sb.Append(" * ");
                sb.Append(sizeof(BoneWeight));
                sb.Append(" [bytes] = ");
                var boneWeightsSize = (ulong)(boneWeights.Length * sizeof(BoneWeight));
                sb.Append(M.HumanReadableBytes(boneWeightsSize));
                sb.AppendLine();
            }

            var blendshapesCount = _mesh.blendShapeCount;
            var blendshapesSize = 0L;
            if (blendshapesCount > 0) {
                var blendshapeSize = sizeof(Vector3) + sizeof(Vector3) + sizeof(Vector3);
                blendshapesSize = blendshapesCount * blendshapeSize * vertexCount;

                if (_verticesDelta?.Length != vertexCount) {
                    _verticesDelta = new Vector3[vertexCount];
                    _normalsDelta = new Vector3[vertexCount];
                    _tangentsDelta = new Vector3[vertexCount];
                }

                var overallEmptyShapes = 0u;

                sb.Append("<b>Blend shapes:</b> ");
                sb.Append(blendshapesCount);
                sb.Append(" * ");
                sb.Append(blendshapeSize);
                sb.Append(" [bytes] * ");
                sb.Append(vertexCount);
                sb.Append(" = ");
                sb.Append(M.HumanReadableBytes(blendshapesSize));
                sb.AppendLine();
                for (var i = 0; i < blendshapesCount; i++) {
                    var blendShapeName = _mesh.GetBlendShapeName(i);
                    _mesh.GetBlendShapeFrameVertices(i, 0, _verticesDelta, _normalsDelta, _tangentsDelta);
                    var emptyShapes = 0u;
                    for (var j = 0; j < vertexCount; j++) {
                        if (_verticesDelta[j].sqrMagnitude < 0.000001f) {
                            ++emptyShapes;
                        }
                    }

                    var fullShapes = vertexCount - emptyShapes;
                    overallEmptyShapes += emptyShapes;

                    sb.Append("   <b>");

                    sb.Append(blendShapeName);

                    sb.Append(":</b> ");
                    sb.Append(M.HumanReadableBytes(blendshapeSize * vertexCount));
                    sb.Append(" uses: ");
                    sb.Append(fullShapes);
                    sb.Append('/');
                    sb.Append(vertexCount);
                    sb.Append(" (");
                    sb.Append((fullShapes * 100f / vertexCount).ToString("F2"));
                    sb.Append("%)");
                    sb.Append(" wasting: ");
                    sb.Append(M.HumanReadableBytes(blendshapeSize * emptyShapes));
                    sb.Append(" frames: ");
                    sb.Append(_mesh.GetBlendShapeFrameCount(i));
                    sb.AppendLine();
                }

                sb.Append(" <b>* Overall empty shapes:</b> ");
                sb.Append(overallEmptyShapes);
                sb.Append('/');
                sb.Append(vertexCount * blendshapesCount);
                sb.Append(" (");
                sb.Append((overallEmptyShapes * 100f / (vertexCount * blendshapesCount)).ToString("F2"));
                sb.Append("%)");
                sb.Append(" wasting: ");
                sb.AppendLine(M.HumanReadableBytes(blendshapeSize * overallEmptyShapes));
                sb.AppendLine();
            }

            var totalSize = verticesSize + indicesSize + bonesSize + blendshapesSize;
            sb.Append("<b>Total size:</b> ");
            sb.Append(M.HumanReadableBytes(totalSize));

            _currentMeshInfo.text = sb.ToString();
        }

        static uint FormatSize(VertexAttributeFormat format) {
            return format switch {
                VertexAttributeFormat.Float32 => sizeof(float),
                VertexAttributeFormat.Float16 => sizeof(ushort),
                VertexAttributeFormat.UNorm8 => sizeof(byte),
                VertexAttributeFormat.SNorm8 => sizeof(sbyte),
                VertexAttributeFormat.UNorm16 => sizeof(ushort),
                VertexAttributeFormat.SNorm16 => sizeof(short),
                VertexAttributeFormat.UInt8 => sizeof(byte),
                VertexAttributeFormat.SInt8 => sizeof(sbyte),
                VertexAttributeFormat.UInt16 => sizeof(ushort),
                VertexAttributeFormat.SInt16 => sizeof(short),
                VertexAttributeFormat.UInt32 => sizeof(uint),
                VertexAttributeFormat.SInt32 => sizeof(int),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }

        static uint AttributeCount(VertexAttribute attribute, uint vertexCount, uint bonesCount) {
            return attribute switch {
                VertexAttribute.Position => vertexCount,
                VertexAttribute.Normal => vertexCount,
                VertexAttribute.Tangent => vertexCount,
                VertexAttribute.Color => vertexCount,
                VertexAttribute.TexCoord0 => vertexCount,
                VertexAttribute.TexCoord1 => vertexCount,
                VertexAttribute.TexCoord2 => vertexCount,
                VertexAttribute.TexCoord3 => vertexCount,
                VertexAttribute.TexCoord4 => vertexCount,
                VertexAttribute.TexCoord5 => vertexCount,
                VertexAttribute.TexCoord6 => vertexCount,
                VertexAttribute.TexCoord7 => vertexCount,
                VertexAttribute.BlendWeight => bonesCount,
                VertexAttribute.BlendIndices => bonesCount,
                _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null)
            };
        }

        [MenuItem("TG/Assets/Kandra/MeshResearch")]
        static void ShowWindow() {
            var window = GetWindow<MeshResearch>();
            window.titleContent = new GUIContent("Mesh research");
            window.Show();
        }
    }
}
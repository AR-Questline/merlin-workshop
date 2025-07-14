using System;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.Kandra.Data;
using Awaken.Kandra.Managers;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.Helpers;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    [BurstCompile]
    public class KandraMeshBaker : AREditorWindow {
        static readonly StringBuilder ReusableStringBuilder = new();
        
        [SerializeField] Mesh mesh;
        [SerializeField] string rootBoneName;

        KandraMesh _result;

        string[] _possibleRootBones = Array.Empty<string>();
        string _error;

        protected override void OnEnable() {
            base.OnEnable();

            AddButton("Bake", Bake, () => mesh && !string.IsNullOrEmpty(rootBoneName));
            AddCustomDrawer(nameof(rootBoneName), DrawRootBone);
        }

        protected override void OnGUI() {
            EditorGUI.BeginChangeCheck();

            base.OnGUI();

            if (EditorGUI.EndChangeCheck()) {
                InputChanged();
            }

            if (!_error.IsNullOrWhitespace()) {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            } else if (_result) {
                EditorGUILayout.ObjectField("Result:", _result, typeof(KandraMesh), false);
            }
        }

        void Bake() {
            if (!TryGetFbxRenderer(mesh, out var fbx, out var renderer, out _error)) {
                return;
            }
            if (!TryGetRootBone(fbx, renderer, ValidateRootBoneName(rootBoneName), out var rootBone, out var rootBoneIndex, out _error)) {
                return;
            }
            _result = Create(mesh, rootBoneIndex, out _);
        }

        void DrawRootBone(SerializedProperty rootBoneNameProp) {
            if (_possibleRootBones.Length == 0) {
                return;
            }
            var selectedIndex = Array.IndexOf(_possibleRootBones, rootBoneNameProp.stringValue);
            selectedIndex = EditorGUILayout.Popup("Root Bone", selectedIndex, _possibleRootBones);
            var selectedValue = selectedIndex < 0 ? "-" : _possibleRootBones[selectedIndex];
            rootBoneNameProp.stringValue = selectedValue;
        }

        public static KandraMesh Create(Mesh mesh, int rootBoneIndex, out float3x4 rootBoneBindpose) {
            var kandraMesh = Create(mesh, rootBoneIndex, out rootBoneBindpose, out var usedBones);
            usedBones.Dispose();
            return kandraMesh;
        }
        
        public static KandraMesh Create(Mesh mesh, int rootBoneIndex, out float3x4 rootBoneBindpose, out UnsafeBitmask usedBones) {
            CalculateBoneData(mesh, rootBoneIndex, out usedBones, out var bonesMap);
            
            SaveMeshData(mesh, usedBones, bonesMap, rootBoneIndex, out var vertexCount, out var bindposesCount, out var blendshapesNames, out rootBoneBindpose);
            SaveIndicesData(mesh, out var indicesCount, out var submeshes);
            
            var bounds = mesh.bounds;
            var localBoundingSphere = new float4(bounds.center, bounds.extents.magnitude);
            
            var kandraMesh = ScriptableObject.CreateInstance<KandraMesh>();
            kandraMesh.meshLocalBounds = mesh.bounds;
            kandraMesh.localBoundingSphere = localBoundingSphere;
            kandraMesh.submeshes = submeshes;
            kandraMesh.blendshapesNames = blendshapesNames;

            kandraMesh.vertexCount = (ushort)vertexCount;
            kandraMesh.indicesCount = (uint)indicesCount;
            kandraMesh.bindposesCount = (ushort)bindposesCount;
            kandraMesh.reciprocalUvDistribution = math.rcp(mesh.GetUVDistributionMetric(0));
            
            bonesMap.Dispose();
            
            SaveKandraAsset(mesh, kandraMesh);
            
            return kandraMesh;
        }
        
        static void CalculateBoneData(Mesh mesh, int rootBone, out UnsafeBitmask usedBones, out UnsafeArray<int> boneMap) {
            usedBones = new UnsafeBitmask((uint)mesh.bindposeCount, ARAlloc.Temp);
            usedBones.Up((uint)rootBone);
            CollectUsedBones(mesh, ref usedBones);
            boneMap = CreateBonesMap(in usedBones);
        }
        
        static void CollectUsedBones(Mesh mesh, ref UnsafeBitmask usedBones) {
            var meshBoneWeights = mesh.boneWeights;
            for (var i = 0; i < meshBoneWeights.Length; i++) {
                usedBones.Up((uint)meshBoneWeights[i].boneIndex0);
                usedBones.Up((uint)meshBoneWeights[i].boneIndex1);
                usedBones.Up((uint)meshBoneWeights[i].boneIndex2);
                usedBones.Up((uint)meshBoneWeights[i].boneIndex3);
            }
        }

        static UnsafeArray<int> CreateBonesMap(in UnsafeBitmask usedBones) {
            var bonesMap = new UnsafeArray<int>(usedBones.ElementsLength, ARAlloc.Temp);
            var bonesMapIndex = 0;
            for (var i = 0u; i < usedBones.ElementsLength; i++) {
                bonesMap[i] = bonesMapIndex;
                if (usedBones[i]) {
                    bonesMapIndex++;
                }
            }

            return bonesMap;
        }
        
        static unsafe void SaveMeshData(Mesh mesh, UnsafeBitmask usedBones, UnsafeArray<int> bonesMap, int rootBone, out int vertexCount, out int bindposesCount, out string[] blendshapesNames, out float3x4 rootBoneBindpose) {
            CheckMeshImportSettings(mesh);

            vertexCount = mesh.vertexCount;

            var localVertices = mesh.vertices;
            var localNormals = mesh.normals;
            var localNormalsPtr = (float3*)UnsafeUtility.PinGCArrayAndGetDataAddress(localNormals, out var localNormalsHandle);
            var localTangents = mesh.tangents;
            var localTangentsPtr = (float4*)UnsafeUtility.PinGCArrayAndGetDataAddress(localTangents, out var localTangentsHandle);
            var localUvs = mesh.uv;
            if (localUvs.Length == 0) {
                localUvs = new Vector2[vertexCount];
            }
            var sourceBoneWeights = mesh.boneWeights;

            var vertices = new UnsafeArray<CompressedVertex>((uint)vertexCount, ARAlloc.Temp);
            var additionalVertexData = new UnsafeArray<AdditionalVertexData>((uint)vertexCount, ARAlloc.Temp);
            var boneWeights = new UnsafeArray<PackedBonesWeights>((uint)vertexCount, ARAlloc.Temp);
            for (var i = 0u; i < vertexCount; i++) {
                var vertex = new Vertex(
                    localVertices[i],
                    localNormals[i],
                    new float3(localTangents[i].x, localTangents[i].y, localTangents[i].z)
                );

                var compressedVertex = new CompressedVertex(vertex);

                vertices[i] = compressedVertex;
                additionalVertexData[i] = new AdditionalVertexData(localUvs[i], localTangents[i].w);
                boneWeights[i] = new PackedBonesWeights(Remap(sourceBoneWeights[i]));
            }

            var originalBindposes = mesh.bindposes;
            var filteredBindposes = originalBindposes.Where(FilterBindposes).ToArray();
            bindposesCount = filteredBindposes.Length;
            var bindposes = new UnsafeArray<float3x4>((uint)filteredBindposes.Length, ARAlloc.Temp);
            for (var i = 0u; i < filteredBindposes.Length; i++) {
                bindposes[i] = PackOrthonormalMatrix(filteredBindposes[i]);
            }

            rootBoneBindpose = PackOrthonormalMatrix(originalBindposes[rootBone]);

            blendshapesNames = new string[mesh.blendShapeCount];

            var blendshapesDeltaVertices = new Vector3[vertexCount];
            var blendshapesDeltaNormals = new Vector3[vertexCount];
            var blendshapesDeltaTangents = new Vector3[vertexCount];

            var blendshapesDeltas = new UnsafeArray<UnsafeArray<PackedBlendshapeDatum>>((uint)mesh.blendShapeCount, ARAlloc.Temp);
            var validBlendshapesCount = 0u;
            for (var i = 0; i < mesh.blendShapeCount; ++i) {
                var blendshapeName = mesh.GetBlendShapeName(i);
                blendshapesNames[i] = blendshapeName;

                mesh.GetBlendShapeFrameVertices(i, 0, blendshapesDeltaVertices, blendshapesDeltaNormals, blendshapesDeltaTangents);
                var verticesPtr = (float3*)UnsafeUtility.PinGCArrayAndGetDataAddress(blendshapesDeltaVertices, out var verticesHandle);
                var normalsPtr = (float3*)UnsafeUtility.PinGCArrayAndGetDataAddress(blendshapesDeltaNormals, out var normalsHandle);
                var tangentsPtr = (float3*)UnsafeUtility.PinGCArrayAndGetDataAddress(blendshapesDeltaTangents, out var tangentsHandle);

                bool isValidBlendshape = !IsBlendshapeEmpty(verticesPtr, vertexCount);

                if (isValidBlendshape) {
                    var blendshapeDeltas = new UnsafeArray<PackedBlendshapeDatum>((uint)vertexCount, ARAlloc.Temp);
                    FillBlendshapeDeltas(vertexCount, ref blendshapeDeltas, verticesPtr, normalsPtr, tangentsPtr, localNormalsPtr, localTangentsPtr);
                    blendshapesDeltas[validBlendshapesCount++] = blendshapeDeltas;
                } else {
                    blendshapesNames[i] = null;
                }

                UnsafeUtility.ReleaseGCObject(verticesHandle);
                UnsafeUtility.ReleaseGCObject(normalsHandle);
                UnsafeUtility.ReleaseGCObject(tangentsHandle);
            }

            var validBlendshapesNames = new string[validBlendshapesCount];
            var validNameIndex = 0;
            for (var i = 0u; i < blendshapesNames.Length; ++i) {
                if (blendshapesNames[i] != null) {
                    validBlendshapesNames[validNameIndex++] = blendshapesNames[i];
                }
            }

            blendshapesNames = validBlendshapesNames;

            UnsafeUtility.ReleaseGCObject(localNormalsHandle);
            UnsafeUtility.ReleaseGCObject(localTangentsHandle);

            // Save all data
            var dataPath = StreamingManager.MeshDataPath(mesh);
            var dataFile = new FileStream(dataPath, FileMode.Create);

            var dataToWrite = new ReadOnlySpan<byte>(vertices.Ptr, (int)(vertices.Length * sizeof(CompressedVertex)));
            dataFile.Write(dataToWrite);
            dataToWrite = new ReadOnlySpan<byte>(additionalVertexData.Ptr, (int)(additionalVertexData.Length * sizeof(AdditionalVertexData)));
            dataFile.Write(dataToWrite);
            dataToWrite = new ReadOnlySpan<byte>(boneWeights.Ptr, (int)(boneWeights.Length * sizeof(PackedBonesWeights)));
            dataFile.Write(dataToWrite);
            dataToWrite = new ReadOnlySpan<byte>(bindposes.Ptr, (int)(bindposes.Length * sizeof(float3x4)));
            dataFile.Write(dataToWrite);
            for (var i = 0u; i < validBlendshapesCount; ++i) {
                dataToWrite = new ReadOnlySpan<byte>(blendshapesDeltas[i].Ptr, (int)(blendshapesDeltas[i].Length * sizeof(PackedBlendshapeDatum)));
                dataFile.Write(dataToWrite);
                blendshapesDeltas[i].Dispose();
            }

            dataFile.Flush();
            dataFile.Dispose();

            vertices.Dispose();
            additionalVertexData.Dispose();
            boneWeights.Dispose();
            bindposes.Dispose();
            blendshapesDeltas.Dispose();

            BoneWeight Remap(BoneWeight boneWeight) {
                return new BoneWeight {
                    boneIndex0 = bonesMap[(uint)boneWeight.boneIndex0],
                    boneIndex1 = bonesMap[(uint)boneWeight.boneIndex1],
                    boneIndex2 = bonesMap[(uint)boneWeight.boneIndex2],
                    boneIndex3 = bonesMap[(uint)boneWeight.boneIndex3],
                    weight0 = boneWeight.weight0,
                    weight1 = boneWeight.weight1,
                    weight2 = boneWeight.weight2,
                    weight3 = boneWeight.weight3,
                };
            }

            bool FilterBindposes(Matrix4x4 _, int index) {
                return usedBones[(uint)index];
            }
        }
        
        static float3x4 PackOrthonormalMatrix(Matrix4x4 input) {
            var matrix = (float4x4)input;
            return new float3x4(matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz, matrix.c3.xyz);
        }

        [BurstCompile]
        static unsafe bool IsBlendshapeEmpty(in float3* vertices, int vertexCount) {
            for (var i = 0; i < vertexCount; ++i) {
                if (math.lengthsq(vertices[i]) > 0.000001f) {
                    return false;
                }
            }

            return true;
        }

        [BurstCompile]
        static unsafe void FillBlendshapeDeltas(int vertexCount, ref UnsafeArray<PackedBlendshapeDatum> blendshapeDeltas,
            float3* verticesPtr, float3* normalsPtr, float3* tangentsPtr, float3* localNormalsPtr, float4* localTangentsPtr) {
            for (var j = 0u; j < vertexCount; ++j) {
                blendshapeDeltas[j] = new PackedBlendshapeDatum(verticesPtr[j],
                    normalsPtr[j],
                    tangentsPtr[j],
                    localNormalsPtr[j],
                    localTangentsPtr[j]);
            }
        }

        static void CheckMeshImportSettings(Mesh mesh) {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mesh));
            if (importer is not ModelImporter modelImporter) {
                return;
            }
            var changed = false;

            if (mesh.isReadable) {
                modelImporter.isReadable = false;
                changed = true;
            }

            bool shouldGenerateNormals = modelImporter.importNormals switch {
                ModelImporterNormals.None => true,
                ModelImporterNormals.Import => mesh.normals.Length == 0,
                _ => false
            };
            if (shouldGenerateNormals) {
                modelImporter.importNormals = ModelImporterNormals.Calculate;
                changed = true;
            }

            bool shouldGenerateTangents = modelImporter.importTangents switch {
                ModelImporterTangents.None => true,
                ModelImporterTangents.Import => mesh.tangents.Length == 0,
                _ => false
            };
            if (shouldGenerateTangents) {
                modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                changed = true;
            }

            if (changed) {
                EditorUtility.SetDirty(modelImporter);
                modelImporter.SaveAndReimport();
            }
        }

        static unsafe void SaveIndicesData(Mesh mesh, out int indicesCount, out SubmeshData[] submeshes) {
            var originalDataArray = MeshUtility.AcquireReadOnlyMeshData(mesh);
            var originalData = originalDataArray[0];
            var originalIndices = originalData.GetIndexData<ushort>();
            indicesCount = originalIndices.Length;
            var submeshCount = originalData.subMeshCount;
            submeshes = new SubmeshData[submeshCount];
            for (var i = 0; i < submeshCount; ++i) {
                var submeshDesc = originalData.GetSubMesh(i);
                submeshes[i] = new SubmeshData {
                    indexStart = (uint)submeshDesc.indexStart,
                    indexCount = (uint)submeshDesc.indexCount,
                };
            }

            var dataPath = StreamingManager.IndicesDataPath(mesh);
            var dataFile = new FileStream(dataPath, FileMode.Create);
            var dataToWrite = new ReadOnlySpan<byte>(originalIndices.GetUnsafeReadOnlyPtr(), indicesCount * sizeof(ushort));
            dataFile.Write(dataToWrite);
            dataFile.Flush();
            dataFile.Dispose();

            originalDataArray.Dispose();
        }

        static void SaveKandraAsset(Mesh mesh, KandraMesh kandraMesh) {
            var originalMeshPath = AssetDatabase.GetAssetPath(mesh);
            var meshName = StreamingManager.KandraMeshName(mesh);
            var newKandraMeshPath = Path.Combine(Path.GetDirectoryName(originalMeshPath), meshName + ".asset");
            AssetDatabase.CreateAsset(kandraMesh, newKandraMeshPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(newKandraMeshPath);
        }
        
        // === Helpers
        
        public static bool TryGetFbxRenderer(Mesh mesh, out GameObject fbx, out SkinnedMeshRenderer renderer, out string error) {
            var meshMainAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(mesh));
            fbx = meshMainAsset as GameObject;
            if (fbx is null) {
                error = $"Mesh {mesh} is not in FBX";
                Log.Important?.Error(error, mesh, LogOption.NoStacktrace);
                renderer = null;
                return false;
            }

            var renderers = fbx.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (!renderers.TryFind(r => r.sharedMesh == mesh, out renderer)) {
                error = $"Mesh {mesh} not found in {fbx}";
                Log.Important?.Error(error, fbx, LogOption.NoStacktrace);
                return false;
            }

            error = null;
            return true;
        }
        
        public static bool TryGetRootBone(GameObject fbx, SkinnedMeshRenderer renderer, string rootBoneName, out Transform rootBone, out int rootBoneIndex, out string error) {
            rootBone = null;
            rootBoneIndex = -1;

            // while searching for root bone we iterate over all transforms in fbx due to variety of setups with empty object not being deforming bone
            var transforms = fbx.GetComponentsInChildren<Transform>();
            if (!transforms.TryFind(t => t.name == rootBoneName, out rootBone)) {
                error = $"Root bone {rootBoneName} not found in {fbx}";
                Log.Important?.Error(error, fbx, LogOption.NoStacktrace);
                return false;
            }

            var bones = renderer.bones;
            for (var i = 0; i < bones.Length; i++) {
                if (bones[i].name == rootBoneName) {
                    rootBoneIndex = i;
                    break;
                }
            }
            if (rootBoneIndex == -1) {
                error = $"Root bone {rootBoneName} not found in {renderer} bones";
                Log.Important?.Error(error, renderer, LogOption.NoStacktrace);
                return false;
            }

            error = null;
            return true;
        }

        [MenuItem("TG/Assets/Kandra/Mesh Baker")]
        static void Open() {
            GetWindow<KandraMeshBaker>().Show();
        }

        void InputChanged() {
            _result = null;
            _possibleRootBones = GetPossibleRootBones(mesh);
        }

        public static string[] GetPossibleRootBones(Mesh mesh) {
            if (mesh == null) {
                return Array.Empty<string>();
            }
            if (!TryGetFbxRenderer(mesh, out var fbx, out _, out _)) {
                return Array.Empty<string>();
            }
            var children = fbx.GetComponentsInChildren<Transform>();
            return ArrayUtils.Select(children, GetPath);
            
            static string GetPath(Transform transform) {
                ReusableStringBuilder.Clear();
                do {
                    var parent = transform.parent;
                    if (parent == null) {
                        ReusableStringBuilder.Insert(0, '/');
                        ReusableStringBuilder.Insert(0, "Other");
                        break;
                    }
                    var name = transform.name;
                    if (name is "Root" or "root" or "Hips" or "hips" or "Pelvis" or "pelvis") {
                        ReusableStringBuilder.Insert(0, '/');
                        ReusableStringBuilder.Insert(0, name);
                        break;
                    }
                    ReusableStringBuilder.Insert(0, '/');
                    ReusableStringBuilder.Insert(0, name);
                    transform = parent;
                } while (true);
                var result = ReusableStringBuilder.ToString(0, ReusableStringBuilder.Length - 1); // skip last '/'
                ReusableStringBuilder.Clear();
                return result;
            }
        }

        public static string ValidateRootBoneName(string nameFromDropdown) {
            if (nameFromDropdown.StartsWith("Other/")) {
                return nameFromDropdown[6..];
            } else {
                return nameFromDropdown;
            }
        }
    }
}
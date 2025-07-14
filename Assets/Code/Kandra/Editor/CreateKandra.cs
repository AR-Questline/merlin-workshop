using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Heroes.SkinnedBones;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.Helpers;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.Kandra.Editor {
    public class CreateKandra : AREditorWindow {
        static readonly Regex BlendshapeRegex = new(@"^m_BlendShapeWeights\.Array\.data\[(\d+)\]$", RegexOptions.Compiled);
        static readonly Dictionary<Shader, Shader> ShaderRedirects = new();

        public GameObject[] targets = Array.Empty<GameObject>();
        GameObject[] _withMissings = Array.Empty<GameObject>();

        Dictionary<Mesh, KandraMesh> _kandraMeshes = new();
        Dictionary<Mesh, float3x4> _rootBoneBindposes = new();

        [MenuItem("TG/Assets/Kandra/Create")]
        public static void ShowWindow() {
            EditorWindow.GetWindow<CreateKandra>().Show();
        }

        protected override void OnEnable() {
            base.OnEnable();

            AddButton("Remove missing scripts", RemoveMissings, () => _withMissings.Length > 0);
            AddButton("Create Kandra", Create, () => targets.Length > 0 && _withMissings.Length == 0);
        }

        protected override void OnGUI() {
            EditorGUI.BeginChangeCheck();

            base.OnGUI();

            if (EditorGUI.EndChangeCheck()) {
                TargetsChanged();
            }
        }

        void Create() {
            StartBatchCreating();

            for (int i = 0; i < targets.Length; i++) {
                GameObject target = targets[i];
                try {
                    ProcessSingleTarget(target);
                } catch (Exception e) {
                    Log.Critical?.Error($"Failed to process {target.name}: {e.Message}");
                    Debug.LogException(e);
                } finally {
                    targets[i] = null;
                }
            }

            targets = Array.Empty<GameObject>();

            FinishBatchCreating();
        }

        public void StartBatchCreating() {
            ShaderRedirects.Clear();
            ShaderRedirects.Add(Shader.Find("HDRP/Lit"), Shader.Find("Shader Graphs/Skinned_Lit"));
            ShaderRedirects.Add(Shader.Find("HDRP/Unlit"), Shader.Find("Shader Graphs/Skinned_Unlit"));
            ShaderRedirects.Add(Shader.Find("HDRP/LitTessellation"), Shader.Find("Shader Graphs/Skinned_Lit_Tessellation"));
        }

        public void FinishBatchCreating() {
            _kandraMeshes.Clear();
            _rootBoneBindposes.Clear();
        }
        
        public void ProcessSingleTarget(GameObject target) {
            var targetTransform = target.transform;
            var skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();

            var skinnedMeshData = new SkinnedMeshData[skinnedRenderers.Length];
            for (var i = 0; i < skinnedRenderers.Length; i++) {
                skinnedMeshData[i] = GetSkinnedMeshData(skinnedRenderers[i]);
            }
            
            var rig = target.AddComponent<KandraRig>();
            var animator = target.GetComponentInChildren<Animator>();

            var allUsedBones = new HashSet<Transform>();
            foreach (var skinnedRenderer in skinnedRenderers) {
                var mesh = skinnedRenderer.sharedMesh;
                if (mesh == null) {
                    Log.Important?.Error($"SkinnedMeshRenderer for {target.name} has no mesh", skinnedRenderer.gameObject);
                    continue;
                }
                var usedBones = new UnsafeBitmask((uint)skinnedRenderer.bones.Length, ARAlloc.Temp);
                CollectUsedBones(mesh, ref usedBones);
                var meshBones = skinnedRenderer.bones;
                for (var index = 0u; index < meshBones.Length; index++) {
                    var bone = meshBones[index];
                    if (usedBones[index]) {
                        // Include all parent bones as we need them for stitching
                        var parentBone = bone.parent;
                        while (parentBone != targetTransform && !allUsedBones.Contains(parentBone)) {
                            allUsedBones.Add(parentBone);
                            parentBone = parentBone.parent;
                        }

                        allUsedBones.Add(meshBones[index]);
                    }
                    if (bone.gameObject.TryGetComponent<AdditionalClothBonesCatalog>(out var catalog)) {
                        DestroyImmediate(catalog, true);
                    }
                }
                allUsedBones.Add(skinnedRenderer.rootBone);
                usedBones.Dispose();
            }

            var allBones = SortBones(allUsedBones, GameObjects.BreadthFirst(targetTransform));

            var parentByBone = new Dictionary<Transform, Transform>();
            foreach (var bone in allBones) {
                var parent = bone.parent;
                if (parent != null) {
                    parentByBone[bone] = parent;
                }
            }

            rig.animator = animator;
            rig.bones = allBones;
            rig.boneNames = allBones.Select(b => new FixedString64Bytes(b.name)).ToArray();
            rig.boneParents = allBones.Select(b =>
                    parentByBone.TryGetValue(b, out var parent)
                        ? (ushort)Array.IndexOf(allBones, parent)
                        : (ushort)0xFFFF)
                .ToArray();
            rig.baseBoneCount = (ushort)allBones.Length;

            for (int i = 0; i < skinnedRenderers.Length; i++) {
                var skinnedRenderer = skinnedRenderers[i];
                var gameObject = skinnedRenderer.gameObject;
                var vfxBodyMarker = gameObject.GetComponent<VFXBodyMarker>();
                if (vfxBodyMarker) {
                    skinnedRenderer.sharedMaterials = Array.Empty<Material>();
                }

                ProcessSkinnedMeshRenderer(skinnedRenderer, rig, skinnedMeshData[i]);
                if (vfxBodyMarker) {
                    vfxBodyMarker.OnValidate();
                    var kandraRenderer = gameObject.GetComponent<KandraRenderer>();
                    kandraRenderer.enabled = false;
                    kandraRenderer.rendererData.materials = Array.Empty<Material>();
                    kandraRenderer.enabled = true;
                }
            }
        }

        SkinnedMeshData GetSkinnedMeshData(SkinnedMeshRenderer skinnedRenderer) {
            var mesh = skinnedRenderer.sharedMesh;

            var submeshesCount = mesh.subMeshCount;
            for (var i = 0; i < submeshesCount; i++) {
                var topology = mesh.GetTopology(i);
                if (topology != MeshTopology.Triangles) {
                    throw new Exception($"Mesh {mesh.name} has unsupported topology {topology}");
                }
            }

            SkinnedMeshData data;
            data.sourceMesh = mesh;

            var bones = skinnedRenderer.bones;
            if (bones.Length == 0) {
                throw new Exception($"SkinnedMeshRenderer {skinnedRenderer.name} has no bones");
            }
            foreach (var bone in bones) {
                if (bone == null) {
                    throw new Exception($"SkinnedRenderer {skinnedRenderer.name} has null bone");
                }
            }
            int localRootBoneIndex = Array.IndexOf(bones, skinnedRenderer.rootBone);
            CalculateBoneData(mesh, localRootBoneIndex, out var usedBones, out var bonesMap);
            data.kandraMesh = KandraMeshBaker.Create(mesh, localRootBoneIndex, out data.rootBoneBindpose);
            
            var skinnedMeshBones = skinnedRenderer.bones;
            data.filteredBones = skinnedMeshBones.Where(FilterBones).ToArray();
            
            usedBones.Dispose();
            bonesMap.Dispose();

            return data;
            
            bool FilterBones(Transform _, int index) {
                return usedBones[(uint)index];
            }
        }

        void ProcessSkinnedMeshRenderer(SkinnedMeshRenderer skinnedRenderer, KandraRig rig, in SkinnedMeshData data) {
            var gameObject = skinnedRenderer.gameObject;

            var materials = skinnedRenderer.sharedMaterials;
            for (var i = materials.Length - 1; i >= 0; i--) {
                var material = materials[i];
                TryRedirectShader(material);
            }

            var renderingFilteringSettings = new KandraRenderer.RendererFilteringSettings {
                renderingLayersMask = skinnedRenderer.renderingLayerMask,
                shadowCastingMode = skinnedRenderer.shadowCastingMode,
            };

            gameObject.SetActive(false);

            var registerData = new KandraRenderer.RendererData {
                EDITOR_sourceMesh = data.sourceMesh,

                rig = rig,
                mesh = data.kandraMesh,
                materials = materials,

                bones = ArrayUtils.Select(data.filteredBones, b => (ushort)Array.IndexOf(rig.bones, b)),
                filteringSettings = renderingFilteringSettings,

                rootBoneMatrix = data.rootBoneBindpose,
                rootBone = (ushort)Array.IndexOf(rig.bones, skinnedRenderer.rootBone),
            };

            var kandraRenderer = gameObject.AddComponent<KandraRenderer>();
            kandraRenderer.rendererData = registerData;

            TryToRetargetSalsa(skinnedRenderer, kandraRenderer);
            TryToRetargetConstantBlendshapes(skinnedRenderer, kandraRenderer);

            DestroyImmediate(skinnedRenderer, true);

            gameObject.SetActive(true);
            EditorUtility.SetDirty(gameObject);
        }

        public void CalculateBoneData(Mesh mesh, int rootBone, out UnsafeBitmask usedBones, out UnsafeArray<int> boneMap) {
            usedBones = new UnsafeBitmask((uint)mesh.bindposeCount, ARAlloc.Temp);
            usedBones.Up((uint)rootBone);
            CollectUsedBones(mesh, ref usedBones);
            boneMap = CreateBonesMap(in usedBones);
        }

        void TryToRetargetSalsa(SkinnedMeshRenderer skinnedRenderer, KandraRenderer kandraRenderer) {
            var controllers = SalsaWithKandraBridgeEditor.CollectControllers(skinnedRenderer);
            if (controllers.Count == 0) {
                return;
            }

            var bridgeMesh = SalsaWithKandraBridgeEditor.CreateBridgeMesh(skinnedRenderer.sharedMesh);
            var bridgeRenderer = CreateBridgeRenderer(skinnedRenderer, bridgeMesh);

            var usedBlendshapes = new HashSet<int>();

            foreach (var controller in controllers) {
                controller.smr = bridgeRenderer;
                usedBlendshapes.Add(controller.blendIndex);
            }

            var bridge = skinnedRenderer.gameObject.AddComponent<SalsaWithKandraBridge>();
            SalsaWithKandraBridge.EditorAccess.BridgeRenderer(bridge) = bridgeRenderer;
            SalsaWithKandraBridge.EditorAccess.KandraRenderer(bridge) = kandraRenderer;
            SalsaWithKandraBridgeEditor.UpdateBlendshapes(bridge, usedBlendshapes);
            EditorUtility.SetDirty(bridge);
        }

        void TryToRetargetConstantBlendshapes(SkinnedMeshRenderer skinnedRenderer, KandraRenderer kandraRenderer) {
            var mesh = skinnedRenderer.sharedMesh;
            if (mesh.blendShapeCount == 0) {
                return;
            }
            ConstantKandraBlendshapes constantBlendshapes = null;
            var blendshapes = new List<ConstantKandraBlendshapes.ConstantBlendshape>();
            var modifications = PrefabUtility.GetPropertyModifications(skinnedRenderer);
            if (modifications == null) {
                return;
            }
            foreach (var mod in modifications) {
                var match = BlendshapeRegex.Match(mod.propertyPath);
                if (!match.Success) {
                    continue;
                }
                if (!float.TryParse(mod.value, out var value)) {
                    continue;
                }
                var blendshapeIndex = int.Parse(match.Groups[1].Value);
                if (mesh.blendShapeCount <= blendshapeIndex) {
                    continue;
                }
                var blendShapeName = mesh.GetBlendShapeName(blendshapeIndex);
                var kandraIndex = kandraRenderer.GetBlendshapeIndex(blendShapeName);
                constantBlendshapes ??= kandraRenderer.gameObject.AddComponent<ConstantKandraBlendshapes>();
                blendshapes.Add(new ConstantKandraBlendshapes.ConstantBlendshape {
                    index = (ushort)kandraIndex,
                    value = value / 100f,
                });
            }
            if (constantBlendshapes) {
                constantBlendshapes.blendshapes = blendshapes.ToArray();
                kandraRenderer.rendererData.constantBlendshapes = constantBlendshapes;
                EditorUtility.SetDirty(constantBlendshapes);
                EditorUtility.SetDirty(kandraRenderer);
            }
        }

        SkinnedMeshRenderer CreateBridgeRenderer(SkinnedMeshRenderer skinnedRenderer, Mesh bridgeMesh) {
            var parent = skinnedRenderer.transform.parent;
            var bridgeGameObject = new GameObject($"{skinnedRenderer.name}_Bridge", typeof(SkinnedMeshRenderer));
            bridgeGameObject.transform.SetParent(parent, false);
            var bridgeRenderer = bridgeGameObject.GetComponent<SkinnedMeshRenderer>();
            bridgeRenderer.sharedMesh = bridgeMesh;
            bridgeRenderer.sharedMaterials = Array.Empty<Material>();
            return bridgeRenderer;
        }

        void CollectUsedBones(Mesh mesh, ref UnsafeBitmask usedBones) {
            var meshBoneWeights = mesh.boneWeights;
            for (var i = 0; i < meshBoneWeights.Length; i++) {
                usedBones.Up((uint)meshBoneWeights[i].boneIndex0);
                usedBones.Up((uint)meshBoneWeights[i].boneIndex1);
                usedBones.Up((uint)meshBoneWeights[i].boneIndex2);
                usedBones.Up((uint)meshBoneWeights[i].boneIndex3);
            }
        }

        UnsafeArray<int> CreateBonesMap(in UnsafeBitmask usedBones) {
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

        KandraTrisCuller.CulledRange[] ConvertBitMaskToRanges(BitMask cullingMask) {
            var ranges = new List<KandraTrisCuller.CulledRange>();

            var rangeStart = -1;
            var rangeLength = 0;

            for (var i = 0; i < cullingMask.Length; ++i) {
                bool shouldBeCulled = cullingMask[i];
                if (shouldBeCulled) {
                    if (rangeStart == -1) {
                        rangeStart = i;
                    }

                    ++rangeLength;
                } else if (rangeStart != -1) {
                    CheckRanges(rangeStart, rangeLength);
                    ranges.Add(new KandraTrisCuller.CulledRange {
                        start = (ushort)rangeStart,
                        length = (ushort)rangeLength
                    });
                    rangeStart = -1;
                    rangeLength = 0;
                }
            }

            if (rangeStart != -1) {
                CheckRanges(rangeStart, rangeLength);
                ranges.Add(new KandraTrisCuller.CulledRange {
                    start = (ushort)rangeStart,
                    length = (ushort)rangeLength
                });
            }

            return ranges.ToArray();
        }

        void CheckRanges(int start, int length) {
            if (length == 0) {
                return;
            }

            if (start is < 0 or > ushort.MaxValue) {
                Log.Minor?.Error("Invalid range start");
            }

            if (length is < 0 or > ushort.MaxValue) {
                Log.Minor?.Error("Invalid range length");
            }

            if ((start + length) is < 0 or > ushort.MaxValue) {
                Log.Minor?.Error("Invalid range end");
            }
        }

        Transform[] SortBones(HashSet<Transform> usedBones, List<Transform> orderedAllTransforms) {
            var result = new Transform[usedBones.Count];

            var rIndex = 0;
            foreach (var transform in orderedAllTransforms) {
                if (usedBones.Contains(transform)) {
                    result[rIndex++] = transform;
                }
            }

            return result;
        }

        public static void TryRedirectShader(Material material) {
            if (!material) {
                return;
            }
            var parent = material.parent;
            while (parent != null) {
                material = parent;
                parent = material.parent;
            }
            var shader = material.shader;
            if (ShaderRedirects.TryGetValue(shader, out var newShader)) {
                material.shader = newShader;
                HDMaterial.ValidateMaterial(material);
            }
        }

        // === Missings
        void TargetsChanged() {
            _withMissings = Array.Empty<GameObject>();
            foreach (var target in targets) {
                CheckMissing(target);
            }
        }

        void CheckMissing(GameObject gameObject) {
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (count > 0) {
                Array.Resize(ref _withMissings, _withMissings.Length + 1);
                _withMissings[^1] = gameObject;
            }
            foreach (Transform childT in gameObject.transform) {
                CheckMissing(childT.gameObject);
            }
        }

        void RemoveMissings() {
            foreach (var target in _withMissings) {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
            }
            _withMissings = Array.Empty<GameObject>();
        }

        struct SkinnedMeshData {
            public Mesh sourceMesh;
            public KandraMesh kandraMesh;
            public Transform[] filteredBones;
            public float3x4 rootBoneBindpose;
        }
    }
}
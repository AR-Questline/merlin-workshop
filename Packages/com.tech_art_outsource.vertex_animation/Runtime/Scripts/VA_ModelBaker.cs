using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System;
using Unity.Mathematics;
using UnityEditor;
#endif

namespace TAO.VertexAnimation.Editor {
    [CreateAssetMenu(fileName = "NewModelBaker", menuName = "VertexAnimation/ModelBaker")]
    public class VA_ModelBaker : ScriptableObject {
        public const int MaxAnimationClipsCount = 16; // 16 because data for each animation clip start offset is stored in 4 Vector4 values   
#if UNITY_EDITOR
        // Input.
        [SerializeField] public GameObject model;
        public AnimationClip[] animationClips;

        [Range(1, 60)]
        public int fps = 24;

        public int targetTextureWidth = 512;
        public int maxTextureDimensions = 2024;
        public bool applyRootMotion = false;
        public bool includeInactive = false;

        public LODSettings lodSettings = new LODSettings();
        public bool applyAnimationBounds = true;
        public bool generateAnimationBook = true;
        public bool generatePrefab = true;
        public Shader materialShader;
        public Shader noVAMaterialShader;

        public bool useInterpolation = true;
        public bool useNormalA = true;

        // Output.
        public GameObject prefab;
        public Texture2DArray positionMap;
        public Material material;
        public Material noVAMaterial;
        public Mesh[] meshes;
        public VA_AnimationBook book;
        public List<VA_Animation> animations = new();

        [System.Serializable]
        public class LODSettings {
            public LODSetting[] lodSettings = new LODSetting[3] { new LODSetting(1, .4f), new LODSetting(.6f, .15f), new LODSetting(.3f, .01f) };

            public float[] GetQualitySettings() {
                float[] q = new float[lodSettings.Length];

                for (int i = 0; i < lodSettings.Length; i++) {
                    q[i] = lodSettings[i].quality;
                }

                return q;
            }

            public float[] GetTransitionSettings() {
                float[] t = new float[lodSettings.Length];

                for (int i = 0; i < lodSettings.Length; i++) {
                    t[i] = lodSettings[i].screenRelativeTransitionHeight;
                }

                return t;
            }

            public int LODCount() {
                return lodSettings.Length;
            }
        }

        [System.Serializable]
        public struct LODSetting {
            [Range(1.0f, 0.0f)]
            public float quality;

            [Range(1.0f, 0.0f)]
            public float screenRelativeTransitionHeight;

            public LODSetting(float q, float t) {
                quality = q;
                screenRelativeTransitionHeight = t;
            }
        }

        private void OnValidate() {
            if (animationClips.Length > MaxAnimationClipsCount) {
                Debug.LogError($"Max animation clips count is {MaxAnimationClipsCount}");
                var newAnimationClips = new AnimationClip[MaxAnimationClipsCount];
                Array.Copy(animationClips, 0, newAnimationClips, 0, MaxAnimationClipsCount);
                animationClips = newAnimationClips;
            }

            ValidateModel();
        }

        public void Bake() {
            if (animationClips.Length == 0) {
                Debug.LogError($"Add animation clips to Bake");
                return;
            }

            var target = Instantiate(model);
            target.name = model.name;

            target.ConbineAndConvertGameObject(includeInactive);
            AnimationBaker.BakedData bakedData = AnimationBaker.Bake(target, animationClips, applyRootMotion, fps);

            positionMap = VA_Texture2DArrayUtils.CreateTextureArray(bakedData.animationClipsPixels,
                targetTextureWidth, maxTextureDimensions, string.Format("{0}_PositionMap", name));
            meshes = bakedData.mesh.GenerateLOD(lodSettings.LODCount(), lodSettings.GetQualitySettings());

            DestroyImmediate(target);

            SaveAssets(bakedData);

            bakedData.Dispose();
        }

        private void SaveAssets(AnimationBaker.BakedData bakedData) {
            AssetDatabaseUtils.RemoveChildAssets(this, new Object[2] { book, material });

            Bounds bounds = new Bounds {
                max = bakedData.maxBounds,
                min = bakedData.minBounds
            };

            for (int i = 0; i < meshes.Length; i++) {
                if (applyAnimationBounds) {
                    meshes[i].bounds = bounds;
                }

                meshes[i].Finalize();
                AssetDatabase.AddObjectToAsset(meshes[i], this);
            }

            AssetDatabase.AddObjectToAsset(positionMap, this);
            AssetDatabase.SaveAssets();

            if (generateAnimationBook) {
                GenerateBook(bakedData);
            }

            GenerateMaterials(bakedData);

            if (generatePrefab) {
                GeneratePrefab(bakedData);
            }


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void GenerateMaterials(AnimationBaker.BakedData bakedData) {
            var animationClipsPixels = bakedData.animationClipsPixels;
            int animationOffsetsArraySize = (int)math.ceil(animationClipsPixels.Length / 4f) * 4;
            var animationsOffsets = new NativeArray<uint>(animationOffsetsArraySize, Allocator.Temp);
            uint processedPixelsCount = 0;
            for (int animationIndex = 0; animationIndex < animationClipsPixels.Length; animationIndex++) {
                animationsOffsets[animationIndex] = processedPixelsCount;
                processedPixelsCount += (uint)animationClipsPixels[animationIndex].Length;
            }

            float4x4 animationOffsets4x4 = default;
            int animationOffsetsVector4Count = animationOffsetsArraySize / 4;
            for (int i = 0; i < animationOffsetsVector4Count; i++) {
                animationOffsets4x4[i] = new float4(animationsOffsets[i * 4], animationsOffsets[i * 4 + 1],
                    animationsOffsets[i * 4 + 2], animationsOffsets[i * 4 + 3]);
            }

            var textureWidth = positionMap.width;
            var textureHeight = positionMap.height;
            // Generate Material
            if (!AssetDatabaseUtils.HasChildAsset(this, material)) {
                material = AnimationMaterial.Create(name, materialShader, positionMap, useNormalA, useInterpolation, 
                    animationOffsets4x4, textureWidth, textureHeight, bakedData.fps, bakedData.mesh.vertexCount);
                AssetDatabase.AddObjectToAsset(material, this);
            } else {
                AnimationMaterial.UpdateMaterial(material, name, materialShader, positionMap, useNormalA, useInterpolation, 
                    animationOffsets4x4, textureWidth, textureHeight, bakedData.fps, bakedData.mesh.vertexCount);
            }

            if (!AssetDatabaseUtils.HasChildAsset(this, noVAMaterial)) {
                noVAMaterial = AnimationMaterial.Create(name + "_NoVA", noVAMaterialShader);
                AssetDatabase.AddObjectToAsset(noVAMaterial, this);
            } else {
                noVAMaterial.name = name + "_NoVA";
                if (noVAMaterial.shader != noVAMaterialShader) {
                    noVAMaterial.shader = noVAMaterialShader;
                }
            }
        }

        private void GeneratePrefab(AnimationBaker.BakedData bakedData) {
            string path = AssetDatabase.GetAssetPath(this);
            int start = path.LastIndexOf('/');
            path = path.Remove(start, path.Length - start);
            path += "/" + name + ".prefab";

            // Generate Prefab
            prefab = AnimationPrefab.Create(path, name, meshes, material, noVAMaterial, lodSettings.GetTransitionSettings(), book);
        }

        private void GenerateBook(AnimationBaker.BakedData bakedData) {
            // Create book.
            if (!book) {
                book = CreateInstance<VA_AnimationBook>();
            }

            book.name = string.Format("{0}_Book", name);
            book.positionMap = positionMap;
            book.animations = new List<VA_Animation>();
            book.TryAddMaterial(material);
            // Save book.
            if (!AssetDatabaseUtils.HasChildAsset(this, book)) {
                AssetDatabase.AddObjectToAsset(book, this);
            }

            var frameTime = 1.0f / fps;
            book.frameTime = frameTime;

            // Create animations.
            var animationClipsCount = bakedData.animationClipsFramesCount.Length;
            for (int clipIndex = 0; clipIndex < animationClipsCount; clipIndex++) {
                string animationName = string.Format("{0}_{1}", name, bakedData.animationClipsNames[clipIndex]);
                var duration = frameTime * bakedData.animationClipsFramesCount[clipIndex];
                var newData = new VA_AnimationData(duration, animationName);

                // Either update existing animation or create a new one.
                if (TryGetAnimationWithName(animationName, out VA_Animation animation)) {
                    animation.SetData(newData, clipIndex);
                } else {
                    animation = CreateInstance<VA_Animation>();
                    animation.name = animationName;
                    animation.SetData(newData, clipIndex);
                    animations.Add(animation);
                }

                book.TryAddAnimation(animation);
            }

            // Save animation objects.
            foreach (var a in animations) {
                AssetDatabaseUtils.TryAddChildAsset(book, a);
            }
        }

        private bool TryGetAnimationWithName(string name, out VA_Animation animation) {
            foreach (var a in animations) {
                if (a != null) {
                    if (a.name == name) {
                        animation = a;
                        return true;
                    }
                }
            }

            animation = null;
            return false;
        }

        public void DeleteSavedAssets() {
            // Remove assets.
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this));
            foreach (var a in assets) {
                if (a != this) {
                    AssetDatabase.RemoveObjectFromAsset(a);
                }
            }

            // Delete prefab.
            string path = AssetDatabase.GetAssetPath(prefab);
            AssetDatabase.DeleteAsset(path);

            // Clear variables.
            prefab = null;
            positionMap = null;
            material = null;
            meshes = null;
            book = null;
            animations = new List<VA_Animation>();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void DeleteUnusedAnimations() {
            if (book != null) {
                // Remove unused animations.
                for (int i = 0; i < animations.Count; i++) {
                    if (!book.animations.Contains(animations[i])) {
                        AssetDatabase.RemoveObjectFromAsset(animations[i]);
                        animations[i] = null;
                    }
                }

                // Remove zero entries.
                animations.RemoveAll(a => a == null);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        void ValidateModel() {
            if (model == null) {
                return;
            }

            var path = AssetDatabase.GetAssetPath(model);
            if (path == null) {
                model = null;
                Debug.LogError($"Object in a field \"{nameof(model)}\" should be an object in Project");
                return;
            }

            var assetImporter = AssetImporter.GetAtPath(path);
            if (!(assetImporter is ModelImporter)) {
                model = null;
                Debug.LogError($"Object in a field \"{nameof(model)}\" should be a Model with an {nameof(ModelImporter)}");
                return;
            }
        }
#endif
    }
}
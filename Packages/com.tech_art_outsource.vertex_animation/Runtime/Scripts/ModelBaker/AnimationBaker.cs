using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TAO.VertexAnimation {
    public static class AnimationBaker {
        [System.Serializable]
        public struct BakedData : IDisposable {
            public Mesh mesh;
            public NativeArray<NativeList<Color>> animationClipsPixels;
            public NativeArray<int> animationClipsFramesCount;
            public string[] animationClipsNames;
            public int fps;
            public Vector3 minBounds;
            public Vector3 maxBounds;

            public void Dispose() {
                foreach (var pixels in animationClipsPixels) {
                    pixels.Dispose();
                }

                animationClipsPixels.Dispose();
                animationClipsFramesCount.Dispose();
                Object.DestroyImmediate(mesh);
            }
        }

        public static BakedData Bake(GameObject model, AnimationClip[] animationClips, bool applyRootMotion, int fps) {
            int animationClipsCount = animationClips.Length;
            BakedData bakedData = new BakedData() {
                mesh = null,
                animationClipsPixels = new NativeArray<NativeList<Color>>(animationClipsCount, Allocator.Persistent),
                animationClipsFramesCount = new NativeArray<int>(animationClipsCount, Allocator.Persistent),
                animationClipsNames = new string[animationClipsCount],
                fps = fps,
                minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                maxBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue)
            };

            SkinnedMeshRenderer skinnedMeshRenderer = model.GetComponent<SkinnedMeshRenderer>();
            var mesh = new Mesh {
                name = string.Format("{0}", model.name)
            };
            skinnedMeshRenderer.BakeMesh(mesh);
            mesh.uv3 = GetVertexIndicesUV(mesh);
            mesh.RecalculateBounds();
            bakedData.mesh = mesh;
            for (int i = 0; i < animationClips.Length; i++) {
                var ac = animationClips[i];
                var framesCount = Mathf.FloorToInt(fps * ac.length);

                Bake(model, ac, framesCount, applyRootMotion,
                    out var minBounds, out var maxBounds, out var pixels);
                bakedData.animationClipsPixels[i] = pixels;
                bakedData.animationClipsFramesCount[i] = framesCount;
                bakedData.animationClipsNames[i] = GetAssetFullName(ac);
                bakedData.minBounds = Vector3.Min(bakedData.minBounds, minBounds);
                bakedData.maxBounds = Vector3.Max(bakedData.maxBounds, maxBounds);
            }

            bakedData.mesh.bounds = new Bounds() {
                max = bakedData.maxBounds,
                min = bakedData.minBounds
            };
            return bakedData;
        }

        static string GetAssetFullName(UnityEngine.Object asset) {
#if UNITY_EDITOR
            // Get the path of the asset
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);

            // Get the parent asset's name (the file name without extension)
            string parentAssetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // Get the main asset at this path
            Object mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);

            // If the asset is the main asset, return just the asset's name
            if (mainAsset == asset) {
                return asset.name;
            }

            // Otherwise, return parentName_thisAssetName for sub-assets
            return $"{parentAssetName}_{asset.name}";
#else
            return asset.name;
#endif
        }

        public static void Bake(GameObject model, AnimationClip animationClip, int framesCount, bool applyRootMotion,
            out Vector3 minBounds, out Vector3 maxBounds, out NativeList<Color> pixels) {
            // Set root motion options.
            if (model.TryGetComponent(out Animator animator)) {
                animator.applyRootMotion = applyRootMotion;
            }

            BakeAnimation(model, animationClip, framesCount,
                out minBounds, out maxBounds, out pixels);
        }

        public static void BakeAnimation(GameObject model, AnimationClip animationClip,
            int framesCount,
            out Vector3 minBounds, out Vector3 maxBounds, out NativeList<Color> pixels) {
            // Create positionMap Texture without MipMaps which is Linear and HDR to store values in a bigger range.
            //Texture2D positionMap = new Texture2D(animationInfo.targetTextureWidth, animationInfo.textureHeight, TextureFormat.RGBAHalf, false, true);
            pixels = new NativeList<Color>(128, Allocator.Persistent);
            // Keep track of min/max bounds.
            minBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            maxBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            // Create instance to sample from.
            GameObject inst = GameObject.Instantiate(model);
            SkinnedMeshRenderer skinnedMeshRenderer = inst.GetComponent<SkinnedMeshRenderer>();

            float frameLength = (animationClip.length / framesCount);
            int firstFrameVertsCount = 0;
            for (int frameIndex = 0; frameIndex < framesCount; frameIndex++) {
                animationClip.SampleAnimation(inst, frameLength * frameIndex);

                Mesh sampledMesh = new Mesh();

                skinnedMeshRenderer.BakeMesh(sampledMesh);

                var verts = new List<Vector3>();
                sampledMesh.GetVertices(verts);
                var normals = new List<Vector3>();
                sampledMesh.GetNormals(normals);
                int vertsCount = verts.Count;
                if (frameIndex == 0) {
                    firstFrameVertsCount = vertsCount;
                }

                for (int v = 0; v < vertsCount; v++) {
                    minBounds = Vector3.Min(minBounds, verts[v]);
                    maxBounds = Vector3.Max(maxBounds, verts[v]);

                    pixels.Add(new Color(verts[v].x, verts[v].y, verts[v].z, VectorUtils.EncodeFloat3ToFloat1(normals[v])));
                }

                Object.DestroyImmediate(sampledMesh);
            }

            //Add first frame to the end to properly loop animation
            pixels.AddRange(pixels.AsArray().GetSubArray(0, firstFrameVertsCount));

            GameObject.DestroyImmediate(inst);
        }

        static Vector2[] GetVertexIndicesUV(Mesh mesh) {
            Vector2[] uv3 = new Vector2[mesh.vertexCount];
            for (int v = 0; v < uv3.Length; v++) {
                uv3[v] = new Vector2(v, 0);
            }

            return uv3;
        }
    }
}
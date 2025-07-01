using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TAO.VertexAnimation {
    public static class VA_Texture2DArrayUtils {
        public static Texture2DArray CreateTextureArray(NativeArray<NativeList<Color>> animationClipsPixels,
            int targetTextureWidth, int maxTextureDimensions,
            string name = "", bool makeNoLongerReadable = true) {
            GetTextureDimensions(animationClipsPixels, targetTextureWidth, maxTextureDimensions,
                out var texturesCount, out var textureWidth, out var textureHeight);
            var textureArray = new Texture2DArray(textureWidth, textureHeight, texturesCount, TextureFormat.RGBAHalf, false, true);
            if (IsValidCopyTexturePlatform() == false) {
                Debug.LogError("Current Platform does not support texture copy");
                return textureArray;
            }

            var textures = new Texture2D[texturesCount];
            for (int i = 0; i < texturesCount; i++) {
                var tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false, true);
                tex.filterMode = FilterMode.Point;
                textures[i] = tex;
            }

            var clipsCount = animationClipsPixels.Length;
            int currentTextureIndex = 0;
            var currentTexturePixel = new int2(0);
            for (int clipIndex = 0; clipIndex < clipsCount; clipIndex++) {
                var clipPixels = animationClipsPixels[clipIndex];
                for (int pixelIndex = 0; pixelIndex < clipPixels.Length; pixelIndex++) {
                    textures[currentTextureIndex].SetPixel(currentTexturePixel.x, currentTexturePixel.y, clipPixels[pixelIndex]);
                    currentTexturePixel.x++;
                    if (currentTexturePixel.x == textureWidth) {
                        currentTexturePixel.x = 0;
                        currentTexturePixel.y++;
                    }

                    if (currentTexturePixel.y == textureHeight) {
                        textures[currentTextureIndex].Apply(false, false);
                        currentTextureIndex++;
                        currentTexturePixel = new(0);
                    }
                }
            }

            for (int textureIndex = 0; textureIndex < texturesCount; textureIndex++) {
                Graphics.CopyTexture(textures[textureIndex], 0, 0, textureArray, textureIndex, 0);
            }

            textureArray.wrapMode = TextureWrapMode.Repeat;
            textureArray.filterMode = FilterMode.Point;
            textureArray.anisoLevel = 1;
            textureArray.name = name;

            textureArray.Apply(false, makeNoLongerReadable);

            return textureArray;
        }

        static void GetTextureDimensions(NativeArray<NativeList<Color>> animationClipsPixels, int targetTextureWidth,
            int maxTextureDimensions, out int texturesCount, out int textureWidth, out int textureHeight) {
            var allPixelsCount = 0;
            foreach (var pixels in animationClipsPixels) {
                allPixelsCount += pixels.Length;
            }

            var textureHeightWithTargetWidth = (int)math.ceil(allPixelsCount / (float)targetTextureWidth);
            if (textureHeightWithTargetWidth <= maxTextureDimensions) {
                texturesCount = 1;
                textureWidth = targetTextureWidth;
                textureHeight = textureHeightWithTargetWidth;
                return;
            }

            var textureHeightWithMaxWidth = (int)math.ceil(allPixelsCount / (float)maxTextureDimensions);
            if (textureHeightWithMaxWidth <= maxTextureDimensions) {
                texturesCount = 1;
                textureWidth = maxTextureDimensions;
                textureHeight = textureHeightWithMaxWidth;
                return;
            }

            int validTextureHeight = textureHeightWithTargetWidth;
            while (validTextureHeight > maxTextureDimensions) {
                validTextureHeight = (int)math.ceil(validTextureHeight * 0.5f);
            }

            texturesCount = (int)math.ceil(textureHeightWithTargetWidth / (float)validTextureHeight);
            textureWidth = targetTextureWidth;
            textureHeight = validTextureHeight;
        }

        public static bool IsValidForTextureArray(Texture2D[] a_textures) {
            if (a_textures == null || a_textures.Length <= 0) {
                Debug.LogError("No textures assigned!");
                return false;
            }

            for (int i = 0; i < a_textures.Length; i++) {
                if (a_textures[i] == null) {
                    Debug.LogError("Texture " + i.ToString() + " not assigned!");
                    return false;
                }

                if (a_textures[0].width != a_textures[i].width || a_textures[0].height != a_textures[i].height) {
                    Debug.LogError("Texture " + a_textures[i].name + " has a different size!");
                    return false;
                }

                if (a_textures[0].format != a_textures[i].format || a_textures[0].graphicsFormat != a_textures[i].graphicsFormat) {
                    Debug.LogError("Texture " + a_textures[i].name + " has a different format/graphics format!");
                    return false;
                }

                if (!a_textures[0].isReadable) {
#if UNITY_EDITOR
                    //Debug.LogWarning("Texture " + a_textures[i].name + " is not readable!");
                    return true;
#else
					Debug.LogError("Texture " + a_textures[i].name + " is not readable!");
					return false;
#endif
                }
            }

            return true;
        }

        public static bool IsValidCopyTexturePlatform() {
            switch (SystemInfo.copyTextureSupport) {
                case UnityEngine.Rendering.CopyTextureSupport.None:
                    Debug.LogError("No CopyTextureSupport on this platform!");
                    return false;
                case UnityEngine.Rendering.CopyTextureSupport.Basic:
                    return true;
                case UnityEngine.Rendering.CopyTextureSupport.Copy3D:
                    return true;
                case UnityEngine.Rendering.CopyTextureSupport.DifferentTypes:
                    return true;
                case UnityEngine.Rendering.CopyTextureSupport.TextureToRT:
                    return true;
                case UnityEngine.Rendering.CopyTextureSupport.RTToTexture:
                    return true;
                default:
#if UNITY_EDITOR
                    return true;
#else
					Debug.LogError("No CopyTextureSupport on this platform!");
					return false;
#endif
            }
        }
    }
}
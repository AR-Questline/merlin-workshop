using System;
using Awaken.TG.Utility.Cameras;
using Awaken.Utility.Cameras;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Utility.Graphics {
    public static class TextureUtils {
        
        // Create Textures

        const float ScaleFactor = 1.5f;
        static int ScreenWidth => (int) (Screen.width / ScaleFactor);
        static int ScreenHeight => (int) (Screen.height / ScaleFactor);

        static Texture2D s_cachedOverlay;
        public static Texture2D GetNotModifiableOverlay {
            get {
                if (s_cachedOverlay == null) {
                    s_cachedOverlay = CreateFullScreenOverlay(false);
                }
                return s_cachedOverlay;
            }
        }  
        
        public static Texture2D CreateFullScreenOverlay(bool withColor = true) => 
            CreateFullScreenTexture(new Color(0f, 0f, 0f, 0.9f), withColor ? TextureFormat.RGBA32 : TextureFormat.Alpha8);
        public static Texture2D CreateFullScreenTexture(Color fillColor, TextureFormat format) => CreateFilledTexture(ScreenWidth, ScreenHeight, fillColor, format);

        static Color[] s_array;
        public static Texture2D CreateFilledTexture(int width, int height, Color baseColor, TextureFormat format = TextureFormat.RGBA32) {
            Texture2D texture = new Texture2D(width, height, format, false);

            // fill texture with baseColor
            Array.Resize(ref s_array, width*height);
            for (int i = 0; i < s_array.Length; i++) {
                s_array[i] = baseColor;
            }
            texture.SetPixels(s_array);
            
            texture.Apply();
            return texture;
        }

        // === Cutting Holes
        [UnityEngine.Scripting.Preserve]
        public static Texture2D CutHoles(this Texture2D texture, Camera camera, Texture2D mask, params RectTransform[] sources) {
            foreach (var source in sources) {
                CutHole(texture, source, camera, mask);
            }
            return texture;
        }
        
        public static Texture2D CutHole(this Texture2D texture, RectTransform source, Camera camera, Texture2D mask = null, float scaleFactor = 1f) {
            Canvas canvas = source.GetComponentInParent<Canvas>();
            Rect rect = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? GetScreenPointRectOverlay(source, canvas) : GetScreenPointRect(source, camera);
            Extract(rect, out int x, out int y, out int width, out int height, scaleFactor);
            return CutHole(texture, x, y, width, height, mask);
        }

        public static Texture2D CutHole(this Texture2D texture, int startX, int startY, int width, int height, Texture2D mask = null) {
            if (mask == null) {
                mask = CreateFilledTexture(width, height, Color.white, TextureFormat.Alpha8);
            }
            
            Texture2D maskCopy = CopyAndResize(mask, width, height);

            // copy mask to correct part of the texture
            Color transparency = new Color(0f, 0f, 0f, 0f);
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            int x = Mathf.Clamp(startX, 0, textureWidth);
            for (; x - startX < width && x < textureWidth; x++) {
                int y = Mathf.Clamp(startY, 0, textureHeight);
                for (; y - startY < height && y < textureHeight; y++) {
                    Color pixel = maskCopy.GetPixel(x - startX, y - startY);
                    Color originalPixel = texture.GetPixel(x, y);
                    Color targetPixel = Color.Lerp(originalPixel, transparency, pixel.a);
                    texture.SetPixel(x, y, targetPixel);
                }
            }

            texture.Apply();
            return texture;
        }
        
        // === Stickers
        public static Texture2D Stick(this Texture2D texture, RectTransform source, Camera camera, Texture2D sticker, float scaleFactor = 1f) {
            Canvas canvas = source.GetComponentInParent<Canvas>();
            Rect rect = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? GetScreenPointRectOverlay(source, canvas) : GetScreenPointRect(source, camera);
            Extract(rect, out int x, out int y, out int width, out int height, scaleFactor);
            return Stick(texture, x, y, width, height, sticker);
        }
        
        public static Texture2D Stick(this Texture2D texture, int startX, int startY, int width, int height, Texture2D sticker) {
            Texture2D stickerCopy = CopyAndResize(sticker, width, height);

            int textureWidth = texture.width;
            int textureHeight = texture.height;

            // copy sprite to correct part of the texture
            int x = Mathf.Clamp(startX, 0, textureWidth);
            for (; x - startX < width && x < textureWidth; x++) {
                int y = Mathf.Clamp(startY, 0, textureHeight);
                for (; y - startY < height && y < textureHeight; y++) {
                    Color pixel = stickerCopy.GetPixel(x - startX, y - startY);
                    Color originalPixel = texture.GetPixel(x, y);
                    Color targetPixel = Color.Lerp(originalPixel, pixel, pixel.a);
                    texture.SetPixel(x, y, targetPixel);
                }
            }

            texture.Apply();
            return texture;
        }

        // === Others
        public static Texture2D ApplyBlur(this Texture2D texture, int factor) {
            int originalWidth = texture.width;
            int originalHeight = texture.height;
            TextureScale.Bilinear(texture, originalWidth / factor, originalHeight / factor);
            TextureScale.Bilinear(texture, originalWidth, originalHeight);
            return texture;
        }

        // === Converters
        [UnityEngine.Scripting.Preserve]
        public static Texture2D ToTexture(this Sprite sprite) {
            if (sprite.rect.width != sprite.texture.width) {
                Texture2D newText = new Texture2D((int) sprite.rect.width, (int) sprite.rect.height);
                Color[] newColors = sprite.texture.GetPixels(Mathf.CeilToInt(sprite.textureRect.x),
                    Mathf.CeilToInt(sprite.textureRect.y),
                    Mathf.CeilToInt(sprite.textureRect.width),
                    Mathf.CeilToInt(sprite.textureRect.height));
                newText.SetPixels(newColors);
                newText.Apply();
                return newText;
            } else
                return sprite.texture;
        }

        public static Sprite ToSprite(this Texture2D texture) {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        
        // === Helpers
        static Texture2D CopyAndResize(Texture2D texture, int width, int height) {
            // make a copy to not mess with original texture
            Texture2D copy = new Texture2D(texture.width, texture.height);
            copy.SetPixels(texture.GetPixels());

            if (width != texture.width || height != texture.height) {
                // resize mask to correct size
                TextureScale.Bilinear(copy, width, height);
            }
            
            copy.Apply();
            return copy;
        }
        
        public static Rect GetScreenPointRect(RectTransform rectTransform, Camera camera) {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

             return new Rect {
                min = camera.WorldToScreenPoint(corners[0]), 
                max = camera.WorldToScreenPoint(corners[2])
            };
        }

        public static Rect GetScreenPointRectOverlay(RectTransform rectTransform, Canvas canvasOverlay) {
            if (canvasOverlay.renderMode != RenderMode.ScreenSpaceOverlay) {
                throw new ArgumentException("Canvas must render on screen space overlay");
            }
            
            Vector3[] corners = new Vector3[4];
            rectTransform.GetLocalCorners(corners);
            
            Vector2 projectionVector = new Vector2(Screen.width, Screen.height);
            Vector2 sizeDelta = ((RectTransform) canvasOverlay.transform).sizeDelta;
            projectionVector.x /= sizeDelta.x * canvasOverlay.transform.localScale.x;
            projectionVector.y /= sizeDelta.y * canvasOverlay.transform.localScale.y;

            Vector2 WorldToScreen(Vector2 world) {
                world.x *= projectionVector.x;
                world.y *= projectionVector.y;
                return world;
            }

            return new Rect {
                min = WorldToScreen(rectTransform.TransformPoint(corners[0])),
                max = WorldToScreen(rectTransform.TransformPoint(corners[2]))
            };
        }

        static void Extract(Rect rect, out int x, out int y, out int width, out int height, float scaleFactor) {
            if (scaleFactor <= 0f) {
                scaleFactor = 1f;
            }
            
            x = Mathf.RoundToInt(rect.x);
            y = Mathf.RoundToInt(rect.y);
            width = Mathf.RoundToInt(rect.width);
            height = Mathf.RoundToInt(rect.height);
            
            // apply local scale factor
            int newWidth = (int)(width * scaleFactor);
            int diffX = newWidth - width;
            x -= diffX / 2;
            width = newWidth;
            
            int newHeight = (int)(height * scaleFactor);
            int diffY = newHeight - height;
            y -= diffY / 2;
            height = newHeight;
            
            // apply global scale factor
            x = (int)(x / ScaleFactor);
            y = (int)(y / ScaleFactor);
            width = (int)(width / ScaleFactor);
            height = (int)(height / ScaleFactor);
        }

        public static RenderTexture CreateRenderTextureFor(RectTransform target, bool canvasSize = false, float renderTextureSizeModifier = 1) {
            var pixelsRect = target.GetPixelsRect();
            var size = new Vector2(pixelsRect.width*renderTextureSizeModifier,
                pixelsRect.height*renderTextureSizeModifier);

            if (canvasSize) {
                var canvasScale = target.GetComponentInParent<Canvas>().transform.localScale.x;
                size /= canvasScale;
            }

            var width = Mathf.RoundToInt(size.x);
            var height = Mathf.RoundToInt(size.y);

            width = Mathf.Abs(width);
            height = Mathf.Abs(height);
            
            // Correct pixels misalignment
            if (Mathf.Abs(width - height) < 3) {
                width = height = Mathf.Max(width, height);
            }

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGBHalf, 0);
            desc.dimension = TextureDimension.Tex2D;
            desc.sRGB = false;
            desc.autoGenerateMips = false;
            desc.useMipMap = false;
            desc.mipCount = 0;
            
            return new RenderTexture(desc);
        }

        public static async UniTask<Texture2D> CreateTexture2DFromCameraPreview(Camera camera, int width, int height, int depth, TextureFormat destinationTextureFormat, RenderTextureFormat renderTextureFormat, MonoBehaviour monoBehaviour) {
            if (camera == null) {
                throw new ArgumentException("Camera has to be not null.");
            }
            
            var sourceTexture = RenderTexture.GetTemporary(width, height, depth, renderTextureFormat);
            await CameraUtils.Render(camera, sourceTexture, monoBehaviour);

            RenderTexture previousRT = RenderTexture.active;
            RenderTexture.active = sourceTexture;
            
            var destinationTexture = new Texture2D(sourceTexture.width, sourceTexture.height, destinationTextureFormat, 1, false, true);
            destinationTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            destinationTexture.Apply();

            RenderTexture.active = previousRT;
            RenderTexture.ReleaseTemporary(sourceTexture);
            camera.targetTexture = null;

            return destinationTexture;
        }
    }
}
using System;
using Awaken.Utility.Files;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Awaken.TG.Editor {
    public class SimpleTexturePacker : OdinEditorWindow {
        [SerializeField] ChannelInput r, g, b, a;
        [SerializeField] DefaultFormat format;
        [SerializeField, Sirenix.OdinInspector.FilePath] string directory;
        [SerializeField] string name;

        [Button]
        void Pack() {
            int width = r.texture.width;
            int height = r.texture.height;
            if (width != g.texture.width || width != b.texture.width || width != a.texture.width || height != g.texture.height || height != b.texture.height || height != a.texture.height) {
                Debug.LogError("Textures must have the same size");
                return;
            }

            var temp = new Texture2D(width, height, format, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
            
            var pixels = new Color[width * height];
            var rPixels = GetPixels(r, temp);
            var gPixels = GetPixels(g, temp);
            var bPixels = GetPixels(b, temp);
            var aPixels = GetPixels(a, temp);
            for (int i = 0; i < pixels.Length; i++) {
                pixels[i] = new Color(GetChannel(rPixels[i], r.channel), GetChannel(gPixels[i], g.channel), GetChannel(bPixels[i], b.channel), GetChannel(aPixels[i], a.channel));
            }
            
            temp.SetPixels(pixels);
            temp.Apply();

            EditorAssetUtil.Create(temp, directory, name);
        }

        Color[] GetPixels(in ChannelInput input, Texture2D tempTexture) {
            var width = input.texture.width;
            var height = input.texture.height;
            var renderTexture = RenderTexture.GetTemporary(width, height, 0);
            UnityEngine.Graphics.Blit(input.texture, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            tempTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tempTexture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            var pixels = tempTexture.GetPixels();
            return pixels;
        }

        [MenuItem("TG/Assets/Textures/SimplePacker")]
        static void Open() {
            GetWindow<SimpleTexturePacker>().Show();
        }

        static float GetChannel(Color color, Channel channel) {
            return channel switch {
                Channel.R => color.r,
                Channel.G => color.g,
                Channel.B => color.b,
                Channel.A => color.a,
                Channel._0 => 0f,
                Channel._1 => 1f,
                Channel.Luminance => (color.r + color.g + color.b) / 3f,
                _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
            };
        }

        [Serializable, InlineProperty]
        struct ChannelInput {
            [HideLabel, HorizontalGroup] public Texture2D texture;
            [HideLabel, HorizontalGroup] public Channel channel;
        }

        enum Channel : byte {
            R, G, B, A, _0, _1, Luminance
        }
    }
}
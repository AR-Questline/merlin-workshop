using System;
using System.IO;
using System.Runtime.InteropServices;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.Utility {
    public static class ClipboardUtility {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport("user32.dll")]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        static extern bool CloseClipboard();

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr hMem);

        const uint CfDib = 8;
        const uint GmemMoveable = 0x0002;
#endif

        public static void CopyImageToClipboard(string imagePath) {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Texture2D texture = null;
            try {
                if (!File.Exists(imagePath)) {
                    Debug.LogError($"Image file not found: {imagePath}");
                    return;
                }

                texture = new Texture2D(1, 1);
                var imageData = File.ReadAllBytes(imagePath);
                texture.LoadImage(imageData);

                CopyTexture2DToClipboard(texture);
                
            } catch (Exception ex) {
                Log.Critical?.Error($"Failed to copy image to clipboard: {ex.Message}");
            } finally {
                if (texture) {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
#else
            Log.Critical?.Error("Clipboard image copying is only supported on Windows");
#endif
        }

        static void CopyTexture2DToClipboard(Texture2D texture) {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var pixels = texture.GetPixels32();
            var width = texture.width;
            var height = texture.height;

            var dibSize = 40 + (width * height * 4);
            var dibData = new byte[dibSize];

            BitConverter.GetBytes(40).CopyTo(dibData, 0);
            BitConverter.GetBytes(width).CopyTo(dibData, 4);
            BitConverter.GetBytes(height).CopyTo(dibData, 8);
            BitConverter.GetBytes((short)1).CopyTo(dibData, 12);
            BitConverter.GetBytes((short)32).CopyTo(dibData, 14);

            var pixelDataOffset = 40;
            var dibIndex = 0;

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    var pixel = pixels[y * width + x];
                    var offset = pixelDataOffset + dibIndex * 4;
                    dibData[offset] = pixel.b;
                    dibData[offset + 1] = pixel.g;
                    dibData[offset + 2] = pixel.r;
                    dibData[offset + 3] = pixel.a;
                    dibIndex++;
                }
            }

            var hGlobal = GlobalAlloc(GmemMoveable, (UIntPtr)dibData.Length);
            if (hGlobal != IntPtr.Zero) {
                var ptr = GlobalLock(hGlobal);
                if (ptr != IntPtr.Zero) {
                    Marshal.Copy(dibData, 0, ptr, dibData.Length);
                    GlobalUnlock(hGlobal);

                    if (OpenClipboard(IntPtr.Zero)) {
                        EmptyClipboard();
                        SetClipboardData(CfDib, hGlobal);
                        CloseClipboard();
                    }
                }
            }
#endif

        }
    }
}
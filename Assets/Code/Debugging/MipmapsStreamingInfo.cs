using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging {
    public sealed class MipmapsStreamingInfo : UGUIWindowDisplay<MipmapsStreamingInfo> {
        protected override bool WithSearch => false;
        protected override bool WithScroll => false;

        protected override void DrawWindow() {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Textures mipmaps streaming info:");
            GUILayout.Label($"Current texture memory: {M.HumanReadableBytes(Texture.currentTextureMemory)}");
            GUILayout.Label($"Desired texture memory: {M.HumanReadableBytes(Texture.desiredTextureMemory)}");
            GUILayout.Label($"Total texture memory: {M.HumanReadableBytes(Texture.totalTextureMemory)}");
            GUILayout.Label($"Target texture memory: {M.HumanReadableBytes(Texture.targetTextureMemory)}");
            GUILayout.Label($"Non-streaming texture memory: {M.HumanReadableBytes(Texture.nonStreamingTextureMemory)}");
            GUILayout.Label($"Streaming mipmap upload count: {Texture.streamingMipmapUploadCount}");
            GUILayout.Label($"Non-streaming texture count: {Texture.nonStreamingTextureCount}");
            GUILayout.Label($"Streaming texture count: {Texture.streamingTextureCount}");
            GUILayout.Label($"Streaming renderer count: {Texture.streamingRendererCount}");
            GUILayout.Label($"Master texture limit: {QualitySettings.globalTextureMipmapLimit}");
            GUILayout.Label($"Pending load: {Texture.streamingTexturePendingLoadCount}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            GUILayout.Label("QualitySettings:", GUILayout.Width(120));
            QualitySettings.streamingMipmapsActive = GUILayout.Toggle(QualitySettings.streamingMipmapsActive, "Streaming Mipmaps Active");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Memory budget", GUILayout.Width(120));
            QualitySettings.streamingMipmapsMemoryBudget = GUILayout.HorizontalSlider(QualitySettings.streamingMipmapsMemoryBudget, 0, 5*1024, GUILayout.Width(256));
            GUILayout.Label($"{QualitySettings.streamingMipmapsMemoryBudget} MB");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max level reduction", GUILayout.Width(120));
            QualitySettings.streamingMipmapsMaxLevelReduction = (int)GUILayout.HorizontalSlider(QualitySettings.streamingMipmapsMaxLevelReduction, 0, 5, GUILayout.Width(256));
            GUILayout.Label($"{QualitySettings.streamingMipmapsMaxLevelReduction}");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Renderers per frame", GUILayout.Width(120));
            QualitySettings.streamingMipmapsRenderersPerFrame = (int)GUILayout.HorizontalSlider(QualitySettings.streamingMipmapsRenderersPerFrame, 0, 1024, GUILayout.Width(256));
            GUILayout.Label($"{QualitySettings.streamingMipmapsRenderersPerFrame}");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max file IO requests", GUILayout.Width(120));
            QualitySettings.streamingMipmapsMaxFileIORequests = (int)GUILayout.HorizontalSlider(QualitySettings.streamingMipmapsMaxFileIORequests, 0, 1024, GUILayout.Width(256));
            GUILayout.Label($"{QualitySettings.streamingMipmapsMaxFileIORequests}");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mipmaps bias", GUILayout.Width(120));
            var newBias = GUILayout.HorizontalSlider(MipmapsBias.bias, MipmapsBiasSetting.Remap1, MipmapsBiasSetting.Remap0, GUILayout.Width(256));
            if (Math.Abs(newBias - MipmapsBias.bias) > 0.001f) {
                World.Only<MipmapsBiasSetting>().SetBias(newBias);
            }
            GUILayout.Label($"{MipmapsBias.bias:f2}");
            GUILayout.EndHorizontal();

            Texture.streamingTextureDiscardUnusedMips = GUILayout.Toggle(Texture.streamingTextureDiscardUnusedMips, "Discard Unused Mips");

            GUILayout.EndVertical();
        }

        [StaticMarvinButton(state: nameof(IsMipmapsStreamingInfoOn))]
        static void ToggleMipmapsStreamingInfo() {
            MipmapsStreamingInfo.Toggle(new UGUIWindowUtils.WindowPositioning(UGUIWindowUtils.WindowPosition.TopLeft, 0.4f, 0.2f));
        }

        static bool IsMipmapsStreamingInfoOn() => MipmapsStreamingInfo.IsShown;
    }
}

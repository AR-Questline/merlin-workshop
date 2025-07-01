using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.UI;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Graphics.Mipmaps {
    public sealed class MipmapsStreamingMasterTexturesDebugWindow : UGUIWindowDisplay<MipmapsStreamingMasterTexturesDebugWindow> {
        const int PageSize = 24;

#if UNITY_EDITOR || AR_DEBUG
        float _debugForced;
#endif
        int _page;

        UnsafeList<int> _expandedIndices;

        protected override void Initialize() {
#if UNITY_EDITOR || AR_DEBUG
            _debugForced = -1;
#endif
            _expandedIndices = new UnsafeList<int>(PageSize, ARAlloc.Persistent);
        }

        protected override void Shutdown() {
            _expandedIndices.Dispose();
            base.Shutdown();
        }

        protected override void DrawWindow() {
            var mipmaps = new MipmapsStreamingMasterTextures.Accessor(MipmapsStreamingMasterTextures.Instance);
            var textures = mipmaps.Textures;

#if UNITY_EDITOR || AR_DEBUG
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{mipmaps.Forced}");
            _debugForced = GUILayout.HorizontalSlider(_debugForced, -1, QualitySettings.streamingMipmapsMaxLevelReduction);
            if (mipmaps.Forced != (int)math.round(_debugForced)) {
                mipmaps.Forced = (int)math.round(_debugForced);
            }
            GUILayout.EndHorizontal();
#endif

            var validIndices = new UnsafeList<int>(textures.Count, ARAlloc.Temp);
            Filter(ref validIndices, textures, mipmaps.Refs);

            TGGUILayout.PagedList(validIndices, DrawTextures, ref _page, PageSize);
            validIndices.Dispose();
        }

        void DrawTextures(int _, int index) {
            var mipmaps = new MipmapsStreamingMasterTextures.Accessor(MipmapsStreamingMasterTextures.Instance);
            var textures = mipmaps.Textures;
            var currentMipmaps = mipmaps.CurrentMipmapsLevels;
            var previousMipmaps = mipmaps.PreviousMipmapsLevels;
            var refs = mipmaps.Refs;
            var expanded = _expandedIndices.Contains(index);
            var newExpanded = TGGUILayout.Foldout(expanded, $"{index}. Texture: {textures[index].name} lvl: {textures[index].loadedMipmapLevel} refs: {refs[index]}");
            if (newExpanded != expanded) {
                if (newExpanded) {
                    _expandedIndices.Add(index);
                } else {
                    var i = _expandedIndices.IndexOf(index);
                    _expandedIndices.RemoveAt(i);
                }
            }

            if (newExpanded) {
                var texture = textures[index];
                using var indentScope = new TGGUILayout.IndentScope();
                GUILayout.Label($"Mipmap count: {texture.mipmapCount}");
                GUILayout.Label($"Current mipmap: {currentMipmaps[index]}");
                GUILayout.Label($"Previous mipmap: {previousMipmaps[index]}");
                GUILayout.Label($"Mipmap bias: {texture.mipMapBias}");
                GUILayout.Label($"Loading mipmap: {texture.loadingMipmapLevel}");
                GUILayout.Label($"Desired mipmap: {texture.desiredMipmapLevel}");
                GUILayout.Label($"Requested mipmap: {texture.requestedMipmapLevel}");
                GUILayout.Label($"Is requested loaded: {texture.IsRequestedMipmapLevelLoaded()}");
            }
        }

        void Filter(ref UnsafeList<int> validIndices, UnsafePinnableList<Texture2D> textures, UnsafeList<ushort> refs) {
            for (int i = 0; i < textures.Count; i++) {
                if (refs[i] != 0 && SearchContext.HasSearchInterest(textures[i].name)) {
                    validIndices.Add(i);
                }
            }
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowMipmapsTexturesWindow() {
            MipmapsStreamingMasterTexturesDebugWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsDebugWindowShown() => MipmapsStreamingMasterTexturesDebugWindow.IsShown;
    }
}

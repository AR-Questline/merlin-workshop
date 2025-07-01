using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.UI;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Awaken.Utility.Graphics.Mipmaps {
    public sealed class MipmapsStreamingMasterMaterialsDebugWindow : UGUIWindowDisplay<MipmapsStreamingMasterMaterialsDebugWindow> {
        const int PageSize = 24;

        int _page;

        protected override void DrawWindow() {
            var mipmaps = new MipmapsStreamingMasterMaterials.Accessor(MipmapsStreamingMasterMaterials.Instance);
            var materials = mipmaps.Materials;

            var validIndices = new UnsafeList<int>(materials.Count, ARAlloc.Temp);

            Filter(ref validIndices, materials, mipmaps.Refs);

            TGGUILayout.PagedList(validIndices, DrawMaterials, ref _page, PageSize);

            validIndices.Dispose();
        }

        void DrawMaterials(int _, int index) {
            var mipmaps = new MipmapsStreamingMasterMaterials.Accessor(MipmapsStreamingMasterMaterials.Instance);
            var materials = mipmaps.Materials;
            var factors = mipmaps.DeferredMipFactors;
            var refs = mipmaps.Refs;
            GUILayout.Label($"{index}. Material: {materials[index].name} factor: {factors[index]} refs: {refs[index]}");
        }

        void Filter(ref UnsafeList<int> validIndices, UnsafePinnableList<Material> materials, UnsafeList<ushort> refs) {
            for (int i = 0; i < materials.Count; i++) {
                if (refs[i] != 0 && SearchContext.HasSearchInterest(materials[i].name)) {
                    validIndices.Add(i);
                }
            }
        }
    }

    static class MipmapsStreamingMasterMaterialsDebugWindowButtons {
        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowMipmapsMaterialsWindow() {
            MipmapsStreamingMasterMaterialsDebugWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsDebugWindowShown() => MipmapsStreamingMasterMaterialsDebugWindow.IsShown;
    }
}

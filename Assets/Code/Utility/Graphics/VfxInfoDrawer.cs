using System.Collections.Generic;
using System.Reflection;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.Utility.Graphics {
    public class VfxInfoDrawer : UGUIWindowDisplay<VfxInfoDrawer> {
#if UNITY_EDITOR
        static readonly MethodInfo GetStorageMemorySizeLongMethod;
#endif

        VisualEffectAsset _inspectedAsset = null;
        List<VisualEffect> _visualEffects = new List<VisualEffect>();

        ImguiTable<VFXBatchedEffectInfo> _batchedInfoTable = new ImguiTable<VFXBatchedEffectInfo>();
        ImguiTable<VisualEffect> _visualEffectsTable = new ImguiTable<VisualEffect>();

        Dictionary<VisualEffectAsset, float> _texturesSizeCache = new Dictionary<VisualEffectAsset, float>();

        protected override bool WithSearch => false;

        static VfxInfoDrawer() {
#if UNITY_EDITOR
            var textureUtilType = typeof(UnityEditor.AssetDatabase).Assembly.GetType("UnityEditor.TextureUtil");
            GetStorageMemorySizeLongMethod = textureUtilType.GetMethod("GetStorageMemorySizeLong", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
#endif
        }

        protected override void Initialize() {
            base.Initialize();
            _batchedInfoTable = new ImguiTable<VFXBatchedEffectInfo>(
                InfoSearchPrediction,
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.Create("Asset", 256, NameDrawer, static i => i.vfxAsset.name),
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Active instances", 96, ImguiTableUtils.FloatDrawer, static i => i.activeInstanceCount),
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Unbatched instances", 96, ImguiTableUtils.FloatDrawer, static i => i.unbatchedInstanceCount),
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Active batches", 96, ImguiTableUtils.FloatDrawer, static i => i.activeBatchCount),
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Inactive batches", 96, ImguiTableUtils.FloatDrawer, static i => i.inactiveBatchCount),
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Total instance capacity", 96, ImguiTableUtils.FloatDrawer, static i => i.totalInstanceCapacity),
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Max instance per batch capacity", 96, ImguiTableUtils.FloatDrawer, static i => i.maxInstancePerBatchCapacity),
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Total GPU size", 128, ImguiTableUtils.MemoryDrawer, static i => i.totalGPUSizeInBytes),
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Total CPU size", 128, ImguiTableUtils.MemoryDrawer, static i => i.totalCPUSizeInBytes)
#if UNITY_EDITOR
                ,
                ImguiTable<VFXBatchedEffectInfo>.ColumnDefinition.CreateNumeric("Textures size", 128, ImguiTableUtils.MemoryDrawer, GetTexturesSize),
                ImguiTableUtils.PingColumn<VFXBatchedEffectInfo, VisualEffectAsset>(static i => i.vfxAsset)
#endif
                );

            _visualEffectsTable = new ImguiTable<VisualEffect>(
                static (o, searchContext) => searchContext.HasSearchInterest(o.gameObject.name),
                ImguiTableUtils.NameColumn<VisualEffect>(256),
                ImguiTableUtils.EnabledColumn<VisualEffect>(),
                ImguiTableUtils.ActiveColumn<VisualEffect>(),
                ImguiTable<VisualEffect>.ColumnDefinition.Create("Layer", 128, LayerDrawer, static ve => LayerMask.LayerToName(ve.gameObject.layer))
#if UNITY_EDITOR
                ,
                ImguiTableUtils.PingColumn<VisualEffect>()
#endif
                );
        }

        protected override void Shutdown() {
            _texturesSizeCache.Clear();
            _inspectedAsset = null;
            _visualEffects.Clear();
            _batchedInfoTable.Dispose();
            _visualEffectsTable.Dispose();
            base.Shutdown();
        }

        protected override void DrawWindow() {
            var infos = new List<VFXBatchedEffectInfo>(18);
            VFXManager.GetBatchedEffectInfos(infos);

            if (GUILayout.Button("Flush empty batches")) {
                VFXManager.FlushEmptyBatches();
            }

            infos.Sort(_batchedInfoTable.Sorter);
            _batchedInfoTable.Draw(infos, Position.height, Scroll.y);

            if (_inspectedAsset) {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Instances of {_inspectedAsset.name}");
                if (GUILayout.Button("X", GUILayout.Width(85))) {
                    _inspectedAsset = null;
                    _visualEffects.Clear();
                }
                GUILayout.EndHorizontal();
            }

            if (_inspectedAsset) {
                var sorterChanged = _visualEffectsTable.Draw(_visualEffects, Position.height, Scroll.y);
                if (sorterChanged) {
                    _visualEffects.Sort(_visualEffectsTable.Sorter);
                }
            }
        }

        static bool InfoSearchPrediction(VFXBatchedEffectInfo info, SearchPattern searchContext) {
            return (searchContext.HasExactSearch("instanced") && info.activeInstanceCount > 0) ||
                   searchContext.HasSearchInterest(info.vfxAsset.name);
        }

        void NameDrawer(in Rect rect, VFXBatchedEffectInfo info) {
            var asset = info.vfxAsset;
            var isSelected = asset == _inspectedAsset;
            if (isSelected) {
                GUI.color = Color.yellow;
            }

            var cellRect = (PropertyDrawerRects)rect;

            if (GUI.Button(cellRect.AllocateLeft(24), "?")) {
                _inspectedAsset = isSelected ? null : asset;
                if (_inspectedAsset) {
                    _visualEffects.Clear();
                    var components = FindObjectsByType<VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var component in components) {
                        if (component.visualEffectAsset == _inspectedAsset) {
                            _visualEffects.Add(component);
                        }
                    }
                } else {
                    _visualEffects.Clear();
                }
                _visualEffects.Sort(_visualEffectsTable.Sorter);
            }
            GUI.Label((Rect)cellRect, asset.name);
        }

        void LayerDrawer(in Rect rect, VisualEffect effect) {
            var layer = effect.gameObject.layer;
            var layerName = LayerMask.LayerToName(layer);
            GUI.Label(rect, layerName);
        }

#if UNITY_EDITOR
        float GetTexturesSize(VFXBatchedEffectInfo info) {
            var asset = info.vfxAsset;

            if (!_texturesSizeCache.TryGetValue(asset, out var size)) {
                var path = UnityEditor.AssetDatabase.GetAssetPath(asset);
                var dependencies = UnityEditor.AssetDatabase.GetDependencies(path, false);
                foreach (var dependencyPath in dependencies) {
                    var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>(dependencyPath);
                    if (texture) {
                        size += (long)GetStorageMemorySizeLongMethod.Invoke(null, new object[] { texture });
                    }
                }
                _texturesSizeCache[asset] = size;
            }

            return size;
        }
#endif

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowVfxWindow() {
            VfxInfoDrawer.Toggle(new UGUIWindowUtils.WindowPositioning(UGUIWindowUtils.WindowPosition.TopLeft, 0.9f, 0.7f));
        }

        static bool IsDebugWindowShown() => VfxInfoDrawer.IsShown;
    }
}

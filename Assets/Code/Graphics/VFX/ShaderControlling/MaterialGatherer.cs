using System;
using System.Collections.Generic;
using Awaken.Kandra;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace Awaken.TG.Graphics.VFX.ShaderControlling {
    public class MaterialGatherer : MonoBehaviour, ISerializationCallbackReceiver {
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
        [SerializeField] string editorName;
#endif
        [SerializeField, InlineProperty, HideLabel, Space(8)] RendererSelector rendererSelector;
        [SerializeField, InlineProperty, HideLabel, Space(8)] MaterialSelector materialSelector;

        [Header("State")]
        [ShowInInspector, ReadOnly, HideInEditorMode] int _refCount;
        [ShowInInspector, ReadOnly, HideInEditorMode] public Material[] Materials { get; private set; }
        
        readonly List<Action> _releaseActions = new();

        public void Gather() {
            _refCount++;
            if (_refCount == 1) {
                GatherImpl();
            }
        }

        public void Release() {
            _refCount--;
            if (_refCount == 0) {
                ReleaseImpl();
            }
        }
        
        void GatherImpl() {
            var gatheredMaterials = new List<Material>();

            if (rendererSelector.IsFromChildren) {
                if (rendererSelector.fromChildren.HasFlagFast(RendererFromChildrenType.Renderer)) {
                    foreach (var r in GetComponentsInChildren<Renderer>()) {
                        GatherFromRenderer(r);
                    }
                } 
                if (rendererSelector.fromChildren.HasFlagFast(RendererFromChildrenType.Decal)) {
                    foreach (var d in GetComponentsInChildren<DecalProjector>()) {
                        GatherFromDecal(d);
                    }
                }
                if (rendererSelector.fromChildren.HasFlagFast(RendererFromChildrenType.UIGraphic)) {
                    foreach (var g in GetComponentsInChildren<Graphic>()) {
                        GatherFromUIGraphic(g);
                    }
                }
                if (rendererSelector.fromChildren.HasFlagFast(RendererFromChildrenType.CustomPass)) {
                    foreach (var cp in GetComponentsInChildren<CustomPassVolume>()) {
                        GatherFromCustomPass(cp);
                    }
                }
                if (rendererSelector.fromChildren.HasFlagFast(RendererFromChildrenType.KandraRenderer)) {
                    foreach (var kr in GetComponentsInChildren<KandraRenderer>()) {
                        GatherFromKandraRenderer(kr);
                    }
                }
            }
            if (rendererSelector.IsByRenderer) {
                foreach (var r in rendererSelector.renderers) {
                    GatherFromRenderer(r);
                }
            }
            if (rendererSelector.IsByDecal) {
                foreach (var d in rendererSelector.decals) {
                    GatherFromDecal(d);
                }
            }
            if (rendererSelector.IsByUIGraphic) {
                foreach (var g in rendererSelector.graphics) {
                    GatherFromUIGraphic(g);
                }
            }
            if (rendererSelector.IsByCustomPass) {
                foreach (var cp in rendererSelector.customPasses) {
                    GatherFromCustomPass(cp);
                }
            }
            if (rendererSelector.IsByKandraRenderer) {
                foreach (var kr in rendererSelector.kandraRenderers) {
                    GatherFromKandraRenderer(kr);
                }
            }

            Materials = gatheredMaterials.ToArray();

            void GatherFromRenderer(Renderer renderer) {
                var localMaterials = renderer.sharedMaterials;
                var previousMaterials = localMaterials;
                for (int i = 0; i < localMaterials.Length; i++) {
                    var material = localMaterials[i];
                    if (materialSelector.IsMatching(material)) {
                        localMaterials = renderer.materials;
                        gatheredMaterials.Add(localMaterials[i]);
                    }
                }
                if (previousMaterials != localMaterials) {
                    _releaseActions.Add(() => {
                        if (renderer) {
                            renderer.materials = previousMaterials;
                        }
                    });
                }
            }
            
            void GatherFromDecal(DecalProjector decal) {
                if (materialSelector.IsMatching(decal.material)) {
                    var previousMaterial = decal.material;
                    var localMaterial = new Material(decal.material);
                    decal.material = localMaterial;
                    gatheredMaterials.Add(localMaterial);
                    _releaseActions.Add(() => {
                        if (decal) {
                            decal.material = previousMaterial;
                        }
                    });
                }
            }
            
            void GatherFromUIGraphic(Graphic graphic) {
                if (materialSelector.IsMatching(graphic.material)) {
                    var previousMaterial = graphic.material;
                    var localMaterial = new Material(graphic.material);
                    graphic.material = localMaterial;
                    gatheredMaterials.Add(localMaterial);
                    _releaseActions.Add(() => {
                        if (graphic) {
                            graphic.material = previousMaterial;
                        }
                    });
                }
            }
            
            void GatherFromCustomPass(CustomPassVolume customPassVolume) {
                foreach (var customPass in customPassVolume.customPasses) {
                    if (customPass is FullScreenCustomPass fullScreenPass){
                        if (materialSelector.IsMatching(fullScreenPass.fullscreenPassMaterial)) {
                            var previousMaterial = fullScreenPass.fullscreenPassMaterial;
                            var localMaterial = new Material(previousMaterial);
                            fullScreenPass.fullscreenPassMaterial = localMaterial;
                            gatheredMaterials.Add(localMaterial);
                            _releaseActions.Add(() => {
                                if (customPassVolume) {
                                    fullScreenPass.fullscreenPassMaterial = previousMaterial;
                                }
                            });
                        }
                    }
                }
            }
            
            void GatherFromKandraRenderer(KandraRenderer kandraRenderer) {
                var localMaterials = kandraRenderer.rendererData.RenderingMaterials;
                for (int i = 0; i < localMaterials.Length; i++) {
                    var material = localMaterials[i];
                    if (materialSelector.IsMatching(material)) {
                        gatheredMaterials.Add(kandraRenderer.UseInstancedMaterial(i));
                        int localI = i;
                        _releaseActions.Add(() => {
                            if (kandraRenderer) {
                                kandraRenderer.UseOriginalMaterial(localI);
                            }
                        });
                    }
                }
            }
        }

        void ReleaseImpl() {
            Materials = null;
            foreach (var action in _releaseActions) {
                action?.Invoke();
            }
            _releaseActions.Clear();
        }

        [Serializable, InlineProperty]
        public struct Handle {
            [HorizontalGroup, HideLabel] public MaterialGatherer gatherer;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
            [HorizontalGroup, HideLabel, ShowInInspector] string Name => gatherer?.editorName;
#endif
        }
        
        [Serializable]
        struct RendererSelector {
            [LabelText("Renderers")] public RendererSelectorType type;
            [ShowIf(nameof(IsFromChildren))] public RendererFromChildrenType fromChildren;
            [ListDrawerSettings(DefaultExpandedState = true), ShowIf(nameof(IsByRenderer))] public Renderer[] renderers;
            [ListDrawerSettings(DefaultExpandedState = true), ShowIf(nameof(IsByDecal))] public DecalProjector[] decals;
            [ListDrawerSettings(DefaultExpandedState = true), ShowIf(nameof(IsByUIGraphic))] public Graphic[] graphics;
            [ListDrawerSettings(DefaultExpandedState = true), ShowIf(nameof(IsByCustomPass))] public CustomPassVolume[] customPasses;
            [ListDrawerSettings(DefaultExpandedState = true), ShowIf(nameof(IsByKandraRenderer))] public KandraRenderer[] kandraRenderers;
            
            public bool IsFromChildren => type.HasFlagFast(RendererSelectorType.FromChildren);
            public bool IsByRenderer => type.HasFlagFast(RendererSelectorType.ByRenderer);
            public bool IsByDecal => type.HasFlagFast(RendererSelectorType.ByDecal);
            public bool IsByUIGraphic => type.HasFlagFast(RendererSelectorType.ByUIGraphic);
            public bool IsByCustomPass => type.HasFlagFast(RendererSelectorType.ByCustomPass);
            public bool IsByKandraRenderer => type.HasFlagFast(RendererSelectorType.ByKandraRenderer);

            public void Cleanup() {
                if (!IsByRenderer) {
                    renderers = Array.Empty<Renderer>();
                }
                if (!IsByDecal) {
                    decals = Array.Empty<DecalProjector>();
                }
                if (!IsByUIGraphic) {
                    graphics = Array.Empty<Graphic>();
                }
                if (!IsByCustomPass) {
                    customPasses = Array.Empty<CustomPassVolume>();
                }
                if (!IsByKandraRenderer) {
                    kandraRenderers = Array.Empty<KandraRenderer>();
                }
            }
        }
        
        [Flags]
        enum RendererSelectorType : byte {
            [UnityEngine.Scripting.Preserve] None = 0,
            FromChildren = 1 << 0,
            ByRenderer = 1 << 1,
            ByDecal = 1 << 2,
            ByUIGraphic = 1 << 3,
            ByCustomPass = 1 << 4,
            ByKandraRenderer = 1 << 5,
        }
        
        [Flags]
        enum RendererFromChildrenType : byte {
            [UnityEngine.Scripting.Preserve] None = 0,
            Renderer = 1 << 0,
            Decal = 1 << 1,
            UIGraphic = 1 << 2,
            CustomPass = 1 << 3,
            KandraRenderer = 1 << 4,
        }
        
        [Serializable]
        struct MaterialSelector {
            [LabelText("Materials")] public MaterialSelectorType type;
            [ListDrawerSettings(DefaultExpandedState = true), ShowIf(nameof(IsByMaterial))] public Material[] materials;
            [ListDrawerSettings(DefaultExpandedState = true), ShowIf(nameof(IsByShader))] public Shader[] shaders;
            
            bool IsByMaterial => type == MaterialSelectorType.ByMaterial;
            bool IsByShader => type == MaterialSelectorType.ByShader;
            
            public bool IsMatching(Material material) {
                return type switch {
                    MaterialSelectorType.None => false,
                    MaterialSelectorType.ByMaterial => Array.IndexOf(materials, material) != -1,
                    MaterialSelectorType.ByShader => Array.IndexOf(shaders, material.shader) != -1,
                    MaterialSelectorType.AllMaterials => true,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            public void Cleanup() {
                if (!IsByMaterial) {
                    materials = Array.Empty<Material>();
                }
                if (!IsByShader) {
                    shaders = Array.Empty<Shader>();
                }
            }
        }
        
        // it's not flag because it's not meant to be combined
        // but it has flag values, so its possible to change it to flag in the future
        enum MaterialSelectorType : byte {
            None = 0,
            ByMaterial = 1 << 0,
            ByShader = 1 << 1,
            AllMaterials =  1 << 2,
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            rendererSelector.Cleanup();
            materialSelector.Cleanup();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() { }
    }
}
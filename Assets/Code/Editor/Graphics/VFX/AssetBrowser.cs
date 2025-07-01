using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Graphics.VFX {
    public class AssetBrowser : OdinMenuEditorWindow {
        [MenuItem("ArtTools/Asset Browser")]
        static void OpenWindow() {
            GetWindow<AssetBrowser>().Show();
        }

        ShaderList _shaderList = new();
        MaterialList _materialList = new();
        VFXList _vfxList = new();

        static bool s_autoRefreshEnabled = true;
        static double s_refreshInterval = 5;
        static double s_nextRefreshTime;

        protected override void OnEnable() {
            base.OnEnable();
            RefreshAll();
            EditorApplication.update += AutoRefresh;
        }

        protected override void OnDisable() {
            base.OnDisable();
            EditorApplication.update -= AutoRefresh;
        }
        
        void AutoRefresh() {
            if (!s_autoRefreshEnabled)
                return;

            bool isHoveredOrFocused = EditorWindow.mouseOverWindow == this || hasFocus;
            if (!isHoveredOrFocused)
                return;

            if (EditorApplication.timeSinceStartup > s_nextRefreshTime) {
                s_nextRefreshTime = EditorApplication.timeSinceStartup + s_refreshInterval;
                RefreshAll();
                Repaint();
            }
        }

        // protected override OdinMenuTree BuildMenuTree() {
        //     var tree = new OdinMenuTree();
        //
        //     tree.Add("Shader", _shaderList, EditorGUIUtility.IconContent("Shader Icon").image);
        //     tree.Add("Material", _materialList, EditorGUIUtility.IconContent("Material Icon").image);
        //     tree.Add("VFX", _vfxList, EditorGUIUtility.IconContent("VisualEffectAsset Icon").image);
        //     tree.Add("Options", new AssetBrowserOptions(), EditorGUIUtility.IconContent("SettingsIcon").image);
        //
        //     return tree;
        // }

        public void RefreshAll() {
            _shaderList.Refresh();
            _materialList.Refresh();
            _vfxList.Refresh();
        }
        
        [Serializable]
        public class AssetBrowserOptions {
            [PropertyOrder(0), ShowInInspector]
            [LabelText("Enable Auto Refresh")]
            [ToggleLeft]
            [OnValueChanged(nameof(OnToggled))]
            public bool AutoRefresh {
                get => AssetBrowser.s_autoRefreshEnabled;
                set => AssetBrowser.s_autoRefreshEnabled = value;
            }

            [PropertyOrder(1), ShowInInspector]
            [LabelText("Refresh Interval (sec)")]
            [SuffixLabel("sec", true)]
            [Range(1, 60)]
            [OnValueChanged(nameof(OnIntervalChanged))]
            public int RefreshInterval {
                get => Mathf.RoundToInt((float)AssetBrowser.s_refreshInterval);
                set => AssetBrowser.s_refreshInterval = Mathf.Clamp(value, 1, 60);
            }

            void OnToggled() {
                ForceRepaint();
            }

            void OnIntervalChanged() {
                AssetBrowser.s_nextRefreshTime = EditorApplication.timeSinceStartup + AssetBrowser.s_refreshInterval;
            }

            void ForceRepaint() {
                if (EditorWindow.HasOpenInstances<AssetBrowser>()) {
                    var window = EditorWindow.GetWindow<AssetBrowser>();
                    // window.ForceMenuTreeRebuild();
                    window.Repaint();
                }
            }
        }
    }

    public static class RendererUtils {
        public static IEnumerable<(Renderer renderer, Material material)> GetAllMaterials(bool excludeHidden) {
            return Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .SelectMany(r => r.sharedMaterials
                    .Where(m => m != null && (!excludeHidden || !m.shader.name.StartsWith("Hidden")))
                    .Select(m => (r, m)));
        }

        public static IEnumerable<(Component drakeRenderer, Material material)> GetAllDrakeRendererMaterials(bool excludeHidden) {
            var drakeRenderers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c => c.GetType().Name == "DrakeMeshRenderer");

            foreach (var dr in drakeRenderers) {
                var matRefsProp = dr.GetType().GetField("materialReferences", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (matRefsProp != null && matRefsProp.GetValue(dr) is UnityEngine.AddressableAssets.AssetReference[] matRefs) {
                    foreach (var matRef in matRefs) {
                        if (matRef == null) continue;
                        var loadedMat = matRef.editorAsset as Material;
                        if (loadedMat != null && (!excludeHidden || !loadedMat.shader.name.StartsWith("Hidden"))) {
                            yield return (dr, loadedMat);
                        }
                    }
                }
            }
        }
    }
    
    public class MaterialUsageCache {
        public class UsageEntry {
            public Object source; // Renderer or DrakeMeshRenderer
            public Material material;
            public bool isDrake;
        }

        public List<UsageEntry> usages = new();

        public void Refresh(bool excludeHidden, bool includeDrake) {
            usages.Clear();

            foreach (var (renderer, material) in RendererUtils.GetAllMaterials(excludeHidden)) {
                usages.Add(new UsageEntry {
                    source = renderer,
                    material = material,
                    isDrake = false
                });
            }

            if (includeDrake) {
                foreach (var (drake, material) in RendererUtils.GetAllDrakeRendererMaterials(excludeHidden)) {
                    usages.Add(new UsageEntry {
                        source = drake,
                        material = material,
                        isDrake = true
                    });
                }
            }
        }
    }

    public class ShaderList {
    [OnValueChanged(nameof(OnExcludeHiddenShadersChanged))] 
    [ToggleLeft] 
    [LabelText("Exclude Hidden Shaders")]
    bool _excludeHiddenShaders = true;

    [OnValueChanged(nameof(OnIncludeDrakeChanged))]
    [ToggleLeft]
    [LabelText("Include DrakeMeshRenderers")]
    bool _includeDrake = true;

    [Searchable]
    [ShowInInspector]
    [TableList(AlwaysExpanded = true, HideToolbar = true, ShowIndexLabels = true)]
    List<ShaderObject> _shaderObjects = new();

    public void Refresh() {
        _shaderObjects.Clear();
        Dictionary<Shader, ShaderObject> shaderProperties = new();

        var cache = new MaterialUsageCache();
        cache.Refresh(_excludeHiddenShaders, _includeDrake);

        foreach (var usage in cache.usages) {
            var shader = usage.material.shader;
            if (shader == null)
                continue;

            if (!shaderProperties.TryGetValue(shader, out var shaderObject)) {
                shaderObject = new ShaderObject {
                    shader = shader,
                    shaderObjects = new List<ObjectUsingShader>()
                };
                shaderProperties[shader] = shaderObject;
            }

            shaderObject.shaderObjects.Add(new ObjectUsingShader {
                gameObject = (usage.source as Component)?.gameObject,
                material = usage.material,
                isDrake = usage.isDrake
            });
        }

        _shaderObjects = shaderProperties.Values.OrderBy(s => s.shader.name).ToList();
    }

    void OnExcludeHiddenShadersChanged() => Refresh();
    void OnIncludeDrakeChanged() => Refresh();
}

    [Serializable]
    public class ShaderObject {
        [TableColumnWidth(256, Resizable = false)]
        public Shader shader;

        [TableColumnWidth(680)] [TableList(ShowIndexLabels = true)]
        public List<ObjectUsingShader> shaderObjects = new();

        [TableColumnWidth(100, Resizable = false)]
        [Button(size: ButtonSizes.Small, Name = "Select All")]
        [GUIColor(0.4f, 0.6f, 1.0f)]
        public void SelectObjects() {
            Selection.objects = shaderObjects.Select(o => (Object)o.gameObject).ToArray();
        }
    }

    [Serializable]
    public class ObjectUsingShader {
        [TableColumnWidth(200)]
        public GameObject gameObject;

        [TableColumnWidth(600)]
        public Material material;

        [ReadOnly]
        [TableColumnWidth(60, Resizable = false)]
        [ShowInInspector, HideLabel]
        public bool isDrake;

        [TableColumnWidth(60, Resizable = false)]
        [Button(size: ButtonSizes.Small, Name = "Focus")]
        [GUIColor(0.4f, 0.6f, 1.0f)]
        public void Focus() {
            Selection.activeGameObject = gameObject;
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }

    public class MaterialList {
        [OnValueChanged(nameof(OnExcludeHiddenMaterialsChanged))]
        [ToggleLeft, LabelText("Exclude Hidden Materials")]
        bool _excludeHiddenMaterials = true;

        [OnValueChanged(nameof(OnIncludeDrakeChanged))]
        [ToggleLeft, LabelText("Include DrakeMeshRenderers")]
        bool _includeDrake = true;

        [Searchable]
        [ShowInInspector]
        [TableList(AlwaysExpanded = true, HideToolbar = true, ShowIndexLabels = true)]
        List<MaterialObject> _materialObjects = new();

        public void Refresh() {
            _materialObjects.Clear();
            var materialProperties = new Dictionary<Material, MaterialObject>();

            var cache = new MaterialUsageCache();
            cache.Refresh(_excludeHiddenMaterials, _includeDrake);

            foreach (var usage in cache.usages) {
                if (!materialProperties.TryGetValue(usage.material, out var matObject)) {
                    matObject = new MaterialObject {
                        material = usage.material,
                        materialObjects = new List<ObjectUsingMaterial>()
                    };
                    materialProperties[usage.material] = matObject;
                }

                matObject.materialObjects.Add(new ObjectUsingMaterial {
                    gameObject = (usage.source as Component)?.gameObject,
                    isDrake = usage.isDrake
                });
            }

            _materialObjects = materialProperties.Values.OrderBy(m => m.material.name).ToList();
        }

        void OnExcludeHiddenMaterialsChanged() => Refresh();
        void OnIncludeDrakeChanged() => Refresh();
    }

    [Serializable]
    public class MaterialObject {
        [TableColumnWidth(256, Resizable = false)]
        public Material material;

        [TableColumnWidth(260)] [TableList(ShowIndexLabels = true)]
        public List<ObjectUsingMaterial> materialObjects = new();

        [TableColumnWidth(100, Resizable = false)]
        [Button(size: ButtonSizes.Small, Name = "Select All")]
        [GUIColor(0.4f, 0.6f, 1.0f)]
        public void SelectObjects() {
            Selection.objects = materialObjects.Select(o => (Object)o.gameObject).ToArray();
        }
    }

    [Serializable]
    public class ObjectUsingMaterial {
        [TableColumnWidth(100)]
        public GameObject gameObject;

        [ReadOnly]
        [TableColumnWidth(60, Resizable = false)]
        [ShowInInspector, HideLabel]
        public bool isDrake;

        [TableColumnWidth(60, Resizable = false)]
        [Button(size: ButtonSizes.Small, Name = "Focus")]
        [GUIColor(0.4f, 0.6f, 1.0f)]
        public void Focus() {
            Selection.activeGameObject = gameObject;
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }

    public class VFXList {
        [Searchable]
        [ShowInInspector]
        [TableList(AlwaysExpanded = true, HideToolbar = true, ShowIndexLabels = true)]
        public List<VisualEffectAssetList> visualEffectAssetLists = new();

        public void Refresh() {
            visualEffectAssetLists.Clear();
            VisualEffect[] visualEffects = Object.FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

            foreach (var group in visualEffects.GroupBy(vfx => vfx.visualEffectAsset)) {
                var asset = group.Key;
                if (asset == null) continue;

                var list = new VisualEffectAssetList {
                    visualEffectAsset = asset,
                    materials = new List<VFXMaterialReference>()
                };

                var materialSet = new HashSet<Material>();

                foreach (var vfx in group) {
                    int particleCount = 0;

                    try {
                        bool safeToRead = vfx.enabled && vfx.isActiveAndEnabled && vfx.gameObject.activeInHierarchy;
                        if (safeToRead) {
                            particleCount = Mathf.Max(0, vfx.aliveParticleCount);
                        }
                    } catch {
                        particleCount = 0;
                    }

                    var obj = new VisualEffectObject {
                        enabled = vfx.enabled,
                        visualEffectObject = vfx.gameObject,
                        particlesCount = particleCount,
                        playRate = vfx.playRate,
                        layerName = LayerMask.LayerToName(vfx.gameObject.layer)
                    };

                    list.visualEffectObjects.Add(obj);

                    var renderer = vfx.GetComponent<Renderer>();
                    if (renderer != null) {
                        foreach (var mat in renderer.sharedMaterials) {
                            if (mat != null && materialSet.Add(mat)) {
                                list.materials.Add(new VFXMaterialReference {
                                    material = mat,
                                    shader = mat.shader
                                });
                            }
                        }
                    }
                }

                visualEffectAssetLists.Add(list);
            }
        }
    }

    [Serializable]
    public class VisualEffectAssetList {
        [TableColumnWidth(256, Resizable = false)]
        public VisualEffectAsset visualEffectAsset;

        [TableColumnWidth(680)]
        [TableList(ShowIndexLabels = true)]
        public List<VisualEffectObject> visualEffectObjects = new();

        [TableList(AlwaysExpanded = false, ShowIndexLabels = true)]
        public List<VFXMaterialReference> materials = new();

        [TableColumnWidth(100, Resizable = false)]
        [Button(size: ButtonSizes.Small, Name = "Select All")]
        [GUIColor(0.4f, 0.6f, 1.0f)]
        public void SelectObjects() {
            Selection.objects = visualEffectObjects.Select(o => (Object)o.visualEffectObject).ToArray();
        }
    }
    
    [Serializable]
    public class VFXMaterialReference {
        [TableColumnWidth(120)]
        public Material material;

        [TableColumnWidth(120)]
        public Shader shader;
    }

    [Serializable]
    [GUIColor(nameof(GetRowColor))]
    public class VisualEffectObject {
        [TableColumnWidth(60, Resizable = false)]
        [ToggleLeft]
        public bool enabled;

        [TableColumnWidth(128)]
        public GameObject visualEffectObject;

        [TableColumnWidth(80, Resizable = false)]
        [ShowInInspector]
        [PropertyTooltip(nameof(GetParticleTooltip))]
        [GUIColor(nameof(GetParticleColor))]
        public string Particles => enabled ? particlesCount.ToString() : "❌ N/A";

        [HideInInspector]
        public int particlesCount;

        string GetParticleTooltip() => enabled
            ? "Alive particle count currently reported by the system."
            : "VisualEffect is disabled — no particles are simulated.";

        Color GetParticleColor() => enabled ? Color.white : new Color(1f, 0.4f, 0.4f);

        [TableColumnWidth(80, Resizable = false)]
        public float playRate;

        [HideInInspector]
        public string layerName;

        [TableColumnWidth(80, Resizable = false)]
        [ShowInInspector]
        [GUIColor(nameof(GetLayerColor))]
        public string Layer => layerName;

        [TableColumnWidth(60, Resizable = false)]
        [Button(size: ButtonSizes.Small, Name = "Focus")]
        [GUIColor(0.4f, 0.6f, 1.0f)]
        public void Focus() {
            Selection.activeGameObject = visualEffectObject;
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Color GetLayerColor() => layerName == "VFX" ? Color.white : Color.red;
        Color GetRowColor() => enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f);
    }
}
using System.Linq;
using System.Text;
using Awaken.Kandra.Debugging;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    public class KandraRendererDebuggerWindow : EditorWindow {
        KandraRendererDebugger _debugger;

        bool _drawInScene = true;
        int _fontSize = 8;
        Verbosity _verbosity = Verbosity.Low;
        bool _merged = true;

        static GUIStyle s_labelStyle;

        [MenuItem("TG/Assets/Kandra/Debug")]
        static void ShowWindow() {
            var window = GetWindow<KandraRendererDebuggerWindow>();
            window.titleContent = new GUIContent("Kandra debug window");
            window.Show();
        }

        void OnEnable() {
            _debugger = new KandraRendererDebugger();
            EditorApplication.update -= Repaint;
            EditorApplication.update += Repaint;

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable() {
            _debugger = null;
            EditorApplication.update -= Repaint;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnGUI() {
            EditorGUILayout.BeginHorizontal();
            _drawInScene = EditorGUILayout.ToggleLeft("Draw in scene", _drawInScene, GUILayout.Width(150));
            _merged = EditorGUILayout.ToggleLeft("Merged info", _merged, GUILayout.Width(150));
            EditorGUILayout.LabelField("Verbosity", GUILayout.Width(80));
            _verbosity = (Verbosity)EditorGUILayout.EnumPopup(_verbosity, GUILayout.Width(70));
            _fontSize = EditorGUILayout.IntSlider("Font size", _fontSize, 3, 14);
            EditorGUILayout.EndHorizontal();

            _debugger.OnGUI();
        }

        void OnSceneGUI(SceneView obj) {
            if (!_drawInScene) {
                return;
            }

            s_labelStyle ??= new GUIStyle(EditorStyles.label) {
                normal = { textColor = Color.white },
                font = Font.CreateDynamicFontFromOSFont("Consolas", 12),
            };

            s_labelStyle.fontSize = _fontSize;

            var sb = new StringBuilder(512);

            if (_merged) {
                var groups = KandraRendererManager.Instance.ActiveRenderers
                    .Where(r => r && KandraRendererManager.Instance.IsRegistered(r.RenderingId))
                    .GroupBy(r => r.rendererData.rig)
                    .Select(g => (g.Key, g.ToArray()))
                    .ToArray();

                using var texts = RentedArray<string>.Borrow(groups.Length);
                using var rects = RentedArray<Rect>.Borrow(groups.Length);

                for (int i = 0; i < groups.Length; i++) {
                    (KandraRig key, KandraRenderer[] renderers) = groups[i];
                    DebugText(key, renderers, sb);
                    texts[i] = sb.ToString();
                    sb.Clear();

                    var avgPosition = Vector3.zero;
                    foreach (var renderer in renderers) {
                        avgPosition += renderer.transform.position;
                    }
                    avgPosition /= renderers.Length;
                    rects[i] = HandlesUtils.LabelRect(avgPosition, texts[i], s_labelStyle);
                }

                Algorithms2D.DistributeRects(rects);

                for (int i = 0; i < groups.Length; i++) {
                    HandlesUtils.Label(rects[i], texts[i], Color.white, s_labelStyle);
                }
            } else {
                var count = 0;
                foreach (var renderer in KandraRendererManager.Instance.ActiveRenderers) {
                    if (!renderer) {
                        continue;
                    }
                    if (!KandraRendererManager.Instance.IsRegistered(renderer.RenderingId)) {
                        continue;
                    }
                    count += 1;
                }

                using var texts = RentedArray<string>.Borrow(count);
                using var rects = RentedArray<Rect>.Borrow(count);

                var i = 0;
                foreach (var renderer in KandraRendererManager.Instance.ActiveRenderers) {
                    if (!renderer) {
                        continue;
                    }
                    if (!KandraRendererManager.Instance.IsRegistered(renderer.RenderingId)) {
                        continue;
                    }
                    DebugText(renderer, sb);
                    texts[i] = sb.ToString();
                    sb.Clear();
                    rects[i] = HandlesUtils.LabelRect(renderer.transform.position, texts[i], s_labelStyle);
                    ++i;
                }
                Algorithms2D.DistributeRects(rects);
                for (int j = 0; j < count; j++) {
                    HandlesUtils.Label(rects[j], texts[j], Color.white, s_labelStyle);
                }
            }
        }

        void DebugText(KandraRenderer renderer, StringBuilder sb) {
            sb.AppendLine($"{renderer.RenderingId:000}. {renderer.name}");

            if (_verbosity == Verbosity.Low) {
                var (ownSize, sharedSize) = renderer.CollectMemorySize();
                sb.AppendLine($"Full: {M.HumanReadableBytes(ownSize+sharedSize),8}");
            } else if (_verbosity == Verbosity.Medium) {
                var (ownSize, sharedSize) = renderer.CollectMemorySize();
                sb.AppendLine($"Full: {M.HumanReadableBytes(ownSize+sharedSize),8}");
                sb.AppendLine($"Own: {M.HumanReadableBytes(ownSize),8}");
                sb.AppendLine($"Shared: {M.HumanReadableBytes(sharedSize),8}");
            } else if (_verbosity == Verbosity.High) {
                var (ownSize, sharedSize) = renderer.CollectMemorySize();
                sb.AppendLine($"Full: {M.HumanReadableBytes(ownSize+sharedSize),8} - Own: {M.HumanReadableBytes(ownSize),8} - Shared: {M.HumanReadableBytes(sharedSize),8}");

                var memory = KandraRendererManager.Instance.RigManager.GetMemoryUsageFor(renderer.rendererData.rig);
                sb.AppendLine($"Rig: {M.HumanReadableBytes(memory)}");

                memory = KandraRendererManager.Instance.MeshManager.GetMemoryUsageFor(renderer.rendererData.mesh);
                sb.AppendLine($"Mesh: {M.HumanReadableBytes(memory)}");

                memory = KandraRendererManager.Instance.BonesManager.GetMemoryUsageFor(renderer.RenderingId);
                sb.AppendLine($"Bones: {M.HumanReadableBytes(memory)}");

                memory = KandraRendererManager.Instance.SkinningManager.GetMemoryUsageFor(renderer.RenderingId);
                sb.AppendLine($"Skinned verts: {M.HumanReadableBytes(memory)}");

                memory = KandraRendererManager.Instance.BlendshapesManager.GetMemoryUsageFor(renderer.rendererData.mesh);
                sb.AppendLine($"Blends: {M.HumanReadableBytes(memory)}");
            } else if (_verbosity == Verbosity.Full) {
                if (KandraRendererManager.Instance.RigManager.TryGetMemoryRegionFor(renderer.rendererData.rig, out var rigMemory)) {
                    var memory = KandraRendererManager.Instance.RigManager.GetMemoryUsageFor(renderer.rendererData.rig);
                    sb.AppendLine($"Rig: {rigMemory} {M.HumanReadableBytes(memory)}");
                } else {
                    sb.AppendLine("No rig region");
                }

                if (KandraRendererManager.Instance.MeshManager.TryGetMeshMemory(renderer.rendererData.mesh, out var meshMemory)) {
                    var memory = KandraRendererManager.Instance.MeshManager.GetMemoryUsageFor(renderer.rendererData.mesh);
                    sb.AppendLine($"Mesh: {meshMemory} {M.HumanReadableBytes(memory)}");
                } else {
                    sb.AppendLine("No mesh region");
                }

                if (KandraRendererManager.Instance.BonesManager.TryGetBonesMemory(renderer.RenderingId, out var bonesMemory)) {
                    var memory = KandraRendererManager.Instance.BonesManager.GetMemoryUsageFor(renderer.RenderingId);
                    sb.AppendLine($"Bones: {bonesMemory} {M.HumanReadableBytes(memory)}");
                } else {
                    sb.AppendLine("No bones region");
                }

                if (KandraRendererManager.Instance.SkinningManager.TryGetSkinnedVerticesMemory(renderer.RenderingId, out var skinnedVertsMemory)) {
                    var memory = KandraRendererManager.Instance.SkinningManager.GetMemoryUsageFor(renderer.RenderingId);
                    sb.AppendLine($"Skinned verts: {skinnedVertsMemory} {M.HumanReadableBytes(memory)}");
                } else {
                    sb.AppendLine("No skinned verts region");
                }

                if (KandraRendererManager.Instance.BlendshapesManager.TryGetBlendshapesData(renderer.rendererData.mesh, out var blendshapesMemory)) {
                    string joined = string.Empty;
                    if (blendshapesMemory.Length > 2) {
                        joined = $"{blendshapesMemory[0]},.{blendshapesMemory.Length - 2}.,{blendshapesMemory[blendshapesMemory.Length - 1]}";
                    } else {
                        joined = string.Join(", ", blendshapesMemory.AsNativeArray());
                    }
                    var memory = KandraRendererManager.Instance.BlendshapesManager.GetMemoryUsageFor(renderer.rendererData.mesh);
                    sb.AppendLine($"Blends: {joined} {M.HumanReadableBytes(memory)}");
                } else {
                    sb.AppendLine("No blends region");
                }
            }
        }

        void DebugText(KandraRig rig, KandraRenderer[] renderers, StringBuilder sb) {
            if (_verbosity == Verbosity.Low) {
                sb.AppendLine(rig.name);
            } else if (_verbosity == Verbosity.Medium) {
                sb.AppendLine(rig.name);
                foreach (var renderer in renderers) {
                    sb.Append($" {renderer.RenderingId:000}");
                }
                sb.AppendLine();
            } else if (_verbosity == Verbosity.High || _verbosity == Verbosity.Full) {
                foreach (var renderer in renderers) {
                    sb.AppendLine($"{renderer.RenderingId:000}. {renderer.name}");
                }
            }

            var fullOwnSize = 0ul;
            var fullSharedSize = 0ul;
            var fullRigSize = KandraRendererManager.Instance.RigManager.GetMemoryUsageFor(rig);
            var fullMeshSize = 0ul;
            var fullBonesSize = 0ul;
            var fullSkinnedVertsSize = 0ul;
            var fullBlendshapesSize = 0ul;

            for (int i = 0; i < renderers.Length; i++) {
                var renderer = renderers[i];
                var (ownSize, sharedSize) = renderer.CollectMemorySize();
                fullOwnSize += ownSize;
                fullSharedSize += sharedSize;

                fullMeshSize += KandraRendererManager.Instance.MeshManager.GetMemoryUsageFor(renderer.rendererData.mesh);
                fullBonesSize += KandraRendererManager.Instance.BonesManager.GetMemoryUsageFor(renderer.RenderingId);
                fullSkinnedVertsSize += KandraRendererManager.Instance.SkinningManager.GetMemoryUsageFor(renderer.RenderingId);
                fullBlendshapesSize += KandraRendererManager.Instance.BlendshapesManager.GetMemoryUsageFor(renderer.rendererData.mesh);
            }

            if (_verbosity == Verbosity.Low) {
                sb.AppendLine($"Full: {M.HumanReadableBytes(fullOwnSize+fullSharedSize),8}");
            } else if (_verbosity == Verbosity.Medium) {
                sb.AppendLine($"Full: {M.HumanReadableBytes(fullOwnSize+fullSharedSize),8}");
                sb.AppendLine($"Own: {M.HumanReadableBytes(fullOwnSize),8}");
                sb.AppendLine($"Shared: {M.HumanReadableBytes(fullSharedSize),8}");
            } else if (_verbosity == Verbosity.High || _verbosity == Verbosity.Full) {
                sb.AppendLine($"Full: {M.HumanReadableBytes(fullOwnSize+fullSharedSize),8} - Own: {M.HumanReadableBytes(fullOwnSize),8} - Shared: {M.HumanReadableBytes(fullSharedSize),8}");

                sb.AppendLine($"Rig: {M.HumanReadableBytes(fullRigSize)}");
                sb.AppendLine($"Mesh: {M.HumanReadableBytes(fullMeshSize)}");
                sb.AppendLine($"Bones: {M.HumanReadableBytes(fullBonesSize)}");
                sb.AppendLine($"Skinned verts: {M.HumanReadableBytes(fullSkinnedVertsSize)}");
                sb.AppendLine($"Blends: {M.HumanReadableBytes(fullBlendshapesSize)}");
            }
        }

        enum Verbosity {
            Low,
            Medium,
            High,
            Full,
        }
    }
}

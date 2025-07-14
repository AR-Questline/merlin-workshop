using System.Collections.Generic;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.Kandra.Debugging {
    public partial class KandraRendererDebugger {
        bool _expandedRenderers;
        Vector2 _renderersScrollPosition;
        List<KandraRenderer> _disabledRenderers = new List<KandraRenderer>();
        OnDemandCache<KandraRenderer, bool> _expandedRenderersCache = new OnDemandCache<KandraRenderer, bool>(static _ => false);

        void RenderersDebug() {
            var renderers = KandraRendererManager.Instance.ActiveRenderers;
            var fullyRegisteredSlots = KandraRendererManager.Instance.FullyRegisteredSlots;

            _expandedRenderers = TGGUILayout.Foldout(_expandedRenderers, $"Active renderers: {fullyRegisteredSlots.CountOnes()}/{renderers.Length}");
            if (!_expandedRenderers) {
                return;
            }

            _renderersScrollPosition = GUILayout.BeginScrollView(_renderersScrollPosition, GUILayout.ExpandHeight(false));

            var toUnregister = KandraRendererManager.Instance.ToUnregister;

            GUILayout.BeginHorizontal();
            GUILayout.Space(Indent);
            GUILayout.BeginVertical();

            for (int i = 0; i < renderers.Length; i++) {
                if (!fullyRegisteredSlots[(uint)i] | toUnregister[(uint)i]) {
                    continue;
                }
                var renderer = renderers[i];

                DrawRenderer(renderer, i);

            }

            if (_disabledRenderers.Count > 0) {
                GUILayout.Label("Disabled renderers:");
                for (int i = _disabledRenderers.Count - 1; i >= 0; i--) {
                    var renderer = _disabledRenderers[i];
                    if (!renderer) {
                        _disabledRenderers.RemoveAt(i);
                        continue;
                    }
                    DrawRenderer(renderer, i);
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        void DrawRenderer(KandraRenderer renderer, int index) {
            var active = renderer.enabled;

            var (ownSize, sharedSize) = renderer.CollectMemorySize();

            GUILayout.BeginHorizontal();
            var expanded = _expandedRenderersCache[renderer];
            var mainLabel = $"{renderer.RenderingId:000}. {renderer.name}:  [Full: {M.HumanReadableBytes(ownSize+sharedSize),8}] [Own: {M.HumanReadableBytes(ownSize),8}] [Shared: {M.HumanReadableBytes(sharedSize),8}]";
            expanded = TGGUILayout.Foldout(expanded, mainLabel);
            _expandedRenderersCache[renderer] = expanded;

            if (active) {
                if (GUILayout.Button("Deactivate", GUILayout.Width(100))) {
                    renderer.enabled = false;
                    _disabledRenderers.Add(renderer);
                }
            } else {
                if (GUILayout.Button("Activate", GUILayout.Width(150))) {
                    renderer.enabled = true;
                    _disabledRenderers.RemoveAt(index);
                }
            }
#if UNITY_EDITOR
            if (GUILayout.Button("Ping", GUILayout.Width(50))) {
                UnityEditor.Selection.activeGameObject = renderer.gameObject;
                UnityEditor.EditorGUIUtility.PingObject(renderer);
            }
#endif
            GUILayout.EndHorizontal();

            if (!expanded) {
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(Indent);
            GUILayout.BeginVertical();
            renderer.DrawMemoryInfo();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}

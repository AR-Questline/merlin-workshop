using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging;
using System.IO;
using Awaken.VendorWrappers.Salsa;
using CrazyMinnow.SALSA;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra {
    [ExecuteInEditMode]
    public class SalsaWithKandraBridge : MonoBehaviour {
        [SerializeField] SkinnedMeshRenderer bridgeRenderer;
        [SerializeField] KandraRenderer kandraRenderer;
        [SerializeField] BlendshapesRedirect[] blendshapesRedirects;

        void Awake() {
            if (kandraRenderer == null || bridgeRenderer == null) {
                Destroy(this);
            }
        }

        void OnEnable() {
            kandraRenderer.EnsureInitialized();
            var hasMissingBlendshapes = false;
#if UNITY_EDITOR
            var missingBlendshapes = default(List<BlendshapesRedirect>);
#endif
            foreach (var blendshapeRedirect in blendshapesRedirects) {
                if (!kandraRenderer.HasBlendshape((ushort)blendshapeRedirect.kandraIndex)) {
                    hasMissingBlendshapes = true;
#if UNITY_EDITOR
                    missingBlendshapes ??= new List<BlendshapesRedirect>();
                    missingBlendshapes.Add(blendshapeRedirect);
#endif
                }
            }

            if (hasMissingBlendshapes) {
#if UNITY_EDITOR
                Log.Important?.Error(
                    $"Missing blendshapes in {kandraRenderer.rendererData.mesh} for {bridgeRenderer.sharedMesh} (instance: {kandraRenderer}):\n\t{string.Join("\n\t", missingBlendshapes)}",
                    this);
#else
                Log.Important?.Error($"Missing blendshapes in {kandraRenderer.rendererData.mesh} for {bridgeRenderer.sharedMesh} (instance: {kandraRenderer})");
#endif
                enabled = false;
            }
        }

        void Update() {
            foreach (var blendshapeRedirect in blendshapesRedirects) {
                kandraRenderer.SetBlendshapeWeight((ushort)blendshapeRedirect.kandraIndex,
                    bridgeRenderer.GetBlendShapeWeight(blendshapeRedirect.sourceIndex) / 100f);
            }
        }

        [Serializable]
        public struct BlendshapesRedirect {
            public int sourceIndex;
            public int kandraIndex;

            public override string ToString() {
                return $"Source: {sourceIndex:00},\tKandra: {kandraIndex:00}";
            }
        }

#if UNITY_EDITOR
        public readonly struct EditorAccess {
            public static ref SkinnedMeshRenderer BridgeRenderer(SalsaWithKandraBridge bridge) => ref bridge.bridgeRenderer;
            public static ref KandraRenderer KandraRenderer(SalsaWithKandraBridge bridge) => ref bridge.kandraRenderer;
            public static ref BlendshapesRedirect[] BlendshapesRedirects(SalsaWithKandraBridge bridge) => ref bridge.blendshapesRedirects;
        }
#endif
    }
}
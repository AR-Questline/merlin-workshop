using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging;
using System.IO;
using Awaken.VendorWrappers.Salsa;
using CrazyMinnow.SALSA;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Awaken.Kandra {
    [ExecuteInEditMode]
    public class SalsaWithKandraBridge : MonoBehaviour {
        [SerializeField] SkinnedMeshRenderer bridgeRenderer;
        [SerializeField] KandraRenderer kandraRenderer;
        [SerializeField, TableList] BlendshapesRedirect[] blendshapesRedirects;

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
                Log.Important?.Error($"Missing blendshapes in {kandraRenderer.rendererData.mesh} for {bridgeRenderer.sharedMesh} (instance: {kandraRenderer}):\n\t{string.Join("\n\t", missingBlendshapes)}", this);
#else
                Log.Important?.Error($"Missing blendshapes in {kandraRenderer.rendererData.mesh} for {bridgeRenderer.sharedMesh} (instance: {kandraRenderer})");
#endif
                enabled = false;
            }
        }

        void Update() {
            foreach (var blendshapeRedirect in blendshapesRedirects) {
                kandraRenderer.SetBlendshapeWeight((ushort)blendshapeRedirect.kandraIndex, bridgeRenderer.GetBlendShapeWeight(blendshapeRedirect.sourceIndex) / 100f);
            }
        }

        [Serializable]
        struct BlendshapesRedirect {
            public int sourceIndex;
            public int kandraIndex;

            public override string ToString() {
                return $"Source: {sourceIndex:00},\tKandra: {kandraIndex:00}";
            }
        }

#if UNITY_EDITOR
        [Button]
        void Refresh() {
            var controllers = CollectControllers(bridgeRenderer);
            if (controllers.Count == 0) {
                Delete();
                return;
            }

            var usedBlendshapes = new HashSet<int>();
            foreach (var controller in controllers) {
                controller.smr = bridgeRenderer;
                usedBlendshapes.Add(controller.blendIndex);
            }

            var bridgeMesh = SalsaWithKandraBridge.CreateBridgeMesh(kandraRenderer.rendererData.EDITOR_sourceMesh);
            bridgeRenderer.sharedMesh = bridgeMesh;

            new EditorAccess(this).UpdateBlendshapes(usedBlendshapes);
        }

        [Button]
        void Delete() {
            DestroyImmediate(bridgeRenderer.gameObject);
            DestroyImmediate(this);
        }

        public static List<InspectorControllerHelperData> CollectControllers(SkinnedMeshRenderer skinnedRenderer) {
            var salsa = skinnedRenderer.GetComponentInParent<Salsa>(true);
            var emoters = skinnedRenderer.GetComponentsInParent<Emoter>(true);
            var eyes = skinnedRenderer.GetComponentInParent<Eyes>(true);

            var controllers = new List<InspectorControllerHelperData>();
            if (salsa) {
                foreach (var viseme in salsa.visemes) {
                    AddExpression(viseme.expData);
                }
            }
            foreach (var emoter in emoters) {
                foreach (var emote in emoter.randomEmotes) {
                    AddExpression(emote.expData);
                }
                foreach (var emote in emoter.emotes) {
                    AddExpression(emote.expData);
                }
            }
            if (eyes) {
                foreach (var eye in eyes.eyes) {
                    AddExpression(eye.expData);
                }
                foreach (var blinklid in eyes.blinklids) {
                    AddExpression(blinklid.expData);
                }
            }
            return controllers;

            void AddExpression(Expression expression) {
                for (int i = 0; i < expression.components.Count; i++) {
                    if (expression.components[i].controlType == ExpressionComponent.ControlType.Shape) {
                        if (expression.controllerVars[i].smr == skinnedRenderer) {
                            controllers.Add(expression.controllerVars[i]);
                        }
                    }
                }
            }
        }

        public static Mesh CreateBridgeMesh(Mesh sourceMesh) {
            const string BasePath = "Assets/3DAssets/Characters/Humans/BaseAssets/Faces/";
            var baseModel = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(sourceMesh));
            var bridgeMeshName = $"{baseModel.name}_{sourceMesh.name}_Bridge";
            var bridgeAssetPath = bridgeMeshName + ".asset";
            var bridgeFinalPath = Path.Combine(BasePath, bridgeAssetPath);
            var vertices = new Vector3[1];

            var tris = new int[3];
            var mesh = new Mesh {
                name = bridgeMeshName,
                vertices = vertices,
                triangles = tris,
            };
            var blendshapesCount = sourceMesh.blendShapeCount;
            for (var i = 0; i < blendshapesCount; i++) {
                Assert.AreEqual(1, sourceMesh.GetBlendShapeFrameCount(i));
                mesh.AddBlendShapeFrame(sourceMesh.GetBlendShapeName(i), sourceMesh.GetBlendShapeFrameWeight(i, 0), vertices, vertices, vertices);
            }
            mesh.Optimize();
            mesh.UploadMeshData(true);
            AssetDatabase.CreateAsset(mesh, bridgeFinalPath);

            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<Mesh>(bridgeFinalPath);
        }

        public readonly struct EditorAccess {
            readonly SalsaWithKandraBridge _bridge;

            public EditorAccess(SalsaWithKandraBridge bridge) {
                _bridge = bridge;
            }

            public void Setup(KandraRenderer kandraRenderer, SkinnedMeshRenderer bridgeRenderer) {
                _bridge.kandraRenderer = kandraRenderer;
                _bridge.bridgeRenderer = bridgeRenderer;
            }

            public void UpdateBlendshapes(HashSet<int> usedBlendshapes) {
                var redirects = new List<BlendshapesRedirect>(usedBlendshapes.Count);
                foreach (var usedBlendshape in usedBlendshapes) {
                    string blendshapeName;
                    try {
                        blendshapeName = _bridge.bridgeRenderer.sharedMesh.GetBlendShapeName(usedBlendshape);
                    } catch (Exception e) {
                        throw new Exception($"SALSA has a blendshape (index {usedBlendshape}) that is not present in the mesh {_bridge.kandraRenderer.rendererData.EDITOR_sourceMesh}");
                    }
                    var kandraIndex = _bridge.kandraRenderer.GetBlendshapeIndex(blendshapeName);
                    if (kandraIndex != -1) {
                        redirects.Add(new BlendshapesRedirect {
                            sourceIndex = usedBlendshape,
                            kandraIndex = kandraIndex
                        });
                    }
                }
                redirects.Sort((lhs, rhs) => lhs.sourceIndex.CompareTo(rhs.sourceIndex));
                _bridge.blendshapesRedirects = redirects.ToArray();
            }
        }
#endif
    }
}
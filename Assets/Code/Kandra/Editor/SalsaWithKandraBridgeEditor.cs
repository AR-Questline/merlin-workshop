using System;
using System.Collections.Generic;
using System.IO;
using Awaken.Utility.UI;
using Awaken.VendorWrappers.Salsa;
using CrazyMinnow.SALSA;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(SalsaWithKandraBridge))]
    public class SalsaWithKandraBridgeEditor : UnityEditor.Editor {
        SerializedProperty _bridgeRendererProperty;
        SerializedProperty _kandraRendererProperty;
        SerializedProperty _blendshapesRedirectsProperty;

        bool _blendshapesRedirectsFoldout;
        ReorderableList _blendshapesRedirectsList;

        void OnEnable() {
            _bridgeRendererProperty = serializedObject.FindProperty("bridgeRenderer");
            _kandraRendererProperty = serializedObject.FindProperty("kandraRenderer");
            _blendshapesRedirectsProperty = serializedObject.FindProperty("blendshapesRedirects");

            _blendshapesRedirectsList = new ReorderableList(serializedObject, _blendshapesRedirectsProperty, true, true, false, false);
            _blendshapesRedirectsList.headerHeight = EditorGUIUtility.singleLineHeight;
            _blendshapesRedirectsList.footerHeight = 0;
            _blendshapesRedirectsList.drawHeaderCallback = rect => {
                    var fullRect = (PropertyDrawerRects)rect;

                    var sourceIndexRect = fullRect.AllocateLeftNormalized(0.5f);
                    var kandraIndexRect = fullRect.Rect;

                    EditorGUI.LabelField(sourceIndexRect, "Source Index");
                    EditorGUI.LabelField(kandraIndexRect, "Kandra Index");
                };
            _blendshapesRedirectsList.drawElementCallback = (rect, index, isActive, isFocused) => {
                var fullRect = (PropertyDrawerRects)rect;
                var sourceIndexRect = fullRect.AllocateLeftNormalized(0.5f);
                var kandraIndexRect = fullRect.Rect;

                var elementProperty = _blendshapesRedirectsProperty.GetArrayElementAtIndex(index);
                var sourceIndexProperty = elementProperty.FindPropertyRelative("sourceIndex");
                var kandraIndexProperty = elementProperty.FindPropertyRelative("kandraIndex");

                EditorGUI.PropertyField(sourceIndexRect, sourceIndexProperty, GUIContent.none);
                EditorGUI.PropertyField(kandraIndexRect, kandraIndexProperty, GUIContent.none);
            };
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_bridgeRendererProperty);
            EditorGUILayout.PropertyField(_kandraRendererProperty);
            _blendshapesRedirectsFoldout = EditorGUILayout.Foldout(_blendshapesRedirectsFoldout, $"Blendshapes Redirects {_blendshapesRedirectsProperty.arraySize}", true);
            if (_blendshapesRedirectsFoldout) {
                EditorGUI.indentLevel++;
                _blendshapesRedirectsList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            var bridge = (SalsaWithKandraBridge)target;
            if (GUILayout.Button("Refresh")) {
                Refresh(bridge);
            }
            if (GUILayout.Button("Delete")) {
                Delete(bridge);
            }
        }

        void Refresh(SalsaWithKandraBridge bridge) {
            var bridgeRenderer = SalsaWithKandraBridge.EditorAccess.BridgeRenderer(bridge);
            var kandraRenderer = SalsaWithKandraBridge.EditorAccess.KandraRenderer(bridge);

            var controllers = CollectControllers(bridgeRenderer);
            if (controllers.Count == 0) {
                Delete(bridge);
                return;
            }

            var usedBlendshapes = new HashSet<int>();
            foreach (var controller in controllers) {
                controller.smr = bridgeRenderer;
                usedBlendshapes.Add(controller.blendIndex);
            }

            var bridgeMesh = CreateBridgeMesh(kandraRenderer.rendererData.EDITOR_sourceMesh);
            bridgeRenderer.sharedMesh = bridgeMesh;

            UpdateBlendshapes(bridge, usedBlendshapes);
        }

        void Delete(SalsaWithKandraBridge bridge) {
            DestroyImmediate(bridge.gameObject);
            DestroyImmediate(this);
        }

        // === Helper Methods
        public static void UpdateBlendshapes(SalsaWithKandraBridge bridge, HashSet<int> usedBlendshapes) {
            var redirects = new List<SalsaWithKandraBridge.BlendshapesRedirect>(usedBlendshapes.Count);
            foreach (var usedBlendshape in usedBlendshapes) {
                string blendshapeName;
                var kandraRenderer = SalsaWithKandraBridge.EditorAccess.KandraRenderer(bridge);
                try {
                    var bridgeRenderer = SalsaWithKandraBridge.EditorAccess.BridgeRenderer(bridge);
                    blendshapeName = bridgeRenderer.sharedMesh.GetBlendShapeName(usedBlendshape);
                } catch (Exception) {
                    throw new Exception($"SALSA has a blendshape (index {usedBlendshape}) that is not present in the mesh {kandraRenderer.rendererData.EDITOR_sourceMesh}");
                }
                var kandraIndex = kandraRenderer.GetBlendshapeIndex(blendshapeName);
                if (kandraIndex != -1) {
                    redirects.Add(new SalsaWithKandraBridge.BlendshapesRedirect {
                        sourceIndex = usedBlendshape,
                        kandraIndex = kandraIndex
                    });
                }
            }
            redirects.Sort((lhs, rhs) => lhs.sourceIndex.CompareTo(rhs.sourceIndex));
            SalsaWithKandraBridge.EditorAccess.BlendshapesRedirects(bridge) = redirects.ToArray();
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
    }
}
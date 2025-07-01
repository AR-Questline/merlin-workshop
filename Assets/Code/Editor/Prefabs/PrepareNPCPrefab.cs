using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Kandra;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Fights.Utils;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Prefabs {
    public class PrepareNPCPrefab : OdinEditorWindow {
        const int RagdollsLayer = RenderLayers.Ragdolls;
        const int AIsLayer = RenderLayers.AIs;
        [InfoBox("This script will:" +
                 "\n- add required scripts to gameObject with animator." +
                 "\n- verify if Ragdoll is present (if not create temporary fix)" +
                 "\n- verify if AlivePrefab exists (if not will create it and add WalkThroughCollider)" +
                 "\n- verify if VFXBodyMarker exists (if not will create it based on CharacterBodyMarker)" +
                 "\n- will mark given Head and Torso objects with correct tag (if they're missing it will add new objects as temporary fix)" +
                 "\n- will mark RootBone with correct tag"
        )]
        [InfoBox("Select visual prefab to prepare - it can be added from project or scene")]
        [SerializeField, OnValueChanged(nameof(UpdatePrefabToPrepare)), ValidateInput(nameof(ValidatePrefab), "Prefab with animator is required")] 
        GameObject prefabToPrepare;
        [InfoBox("Select Head and Torso bones" +
                 "\nif filled automatically: they're already set up" +
                 "\nif null: placeholder gameObjects will be created under root bone")]
        [SerializeField] Transform head;
        [SerializeField] Transform torso;
        [InfoBox("Simplified mesh for VFXes" +
                 "\nif null: the default mesh will be used")]
        [SerializeField] KandraMesh vfxMesh;
        
        bool? _result;

        [InfoBox("Prefab prepared successfully", InfoMessageType.Warning, nameof(ShowSuccess))]
        [InfoBox("Prefab preparation failed", InfoMessageType.Error, nameof(ShowFail))]
        [ShowIf(nameof(ShowLogInfo)), ShowInInspector, Multiline(10), HideLabel]
        string _logInfo;
        bool ShowSuccess => _result is true;
        bool ShowFail => _result is false;
        bool ShowLogInfo => !string.IsNullOrEmpty(_logInfo);
        
        [MenuItem("TG/Assets/Prefabs/Prepare NPC Prefab")]
        public static void ShowWindow() {
            GetWindow<PrepareNPCPrefab>("Prepare NPC Prefab").Show();
        }

        void UpdatePrefabToPrepare() {
            if (prefabToPrepare == null) {
                head = null;
                torso = null;
                return;
            }
            head = prefabToPrepare.FindChildWithTagRecursively("Head");
            torso = prefabToPrepare.FindChildWithTagRecursively("Torso");
        }

        bool ValidatePrefab() {
            return prefabToPrepare != null && prefabToPrepare.GetComponentInChildren<Animator>() != null;
        }
        
        [Button]
        void Prepare() {
            _logInfo = null;
            _result = null;
            InfoLog("Start Prepare");
            if (prefabToPrepare == null) {
                FixItNowLog("No prefab to prepare selected");
                _result = false;
                return;
            }
            bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(prefabToPrepare);
            var prefabToModify = isPrefabAsset ? PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefabToPrepare)) : prefabToPrepare;
            var animator = prefabToModify.GetComponentInChildren<Animator>();
            if (animator == null) {
                FixItNowLog("No animator found in children");
                _result = false;
                return;
            }
            PrepareAnimatorBasedComponents(animator);
            Bounds? bounds = null;
            var animatorTransform = animator.transform;
            Transform rootBone = null;
            bool ignoreAlivePrefab = false;
            bool anyRagdollFound = false;
            
            var kandraRenderers = new List<KandraRenderer>();
            for (int i = 0; i < animatorTransform.childCount; i++) {
                var child = animatorTransform.GetChild(i);
                if (child.CompareTag("AlivePrefab")) {
                    ignoreAlivePrefab = true;
                }

                var tempRenderers = child.gameObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in tempRenderers) {
                    bounds.Encapsulate(renderer.bounds);
                    if (renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                        FixItNowLog($"Has skinned mesh renderer: {skinnedMeshRenderer.name}", skinnedMeshRenderer.gameObject);
                    }
                }
                var tempKandraRenderers = child.gameObject.GetComponentsInChildren<KandraRenderer>();
                kandraRenderers.AddRange(tempKandraRenderers);

                if (child.gameObject.layer == RagdollsLayer) {
                    anyRagdollFound = true;
                }
            }
            if (kandraRenderers.Count == 0) {
                FixItNowLog("No kandra renderers found");
                _result = false;
                return;
            }

            var lastKandraRenderer = kandraRenderers.Last();
            var rig = lastKandraRenderer.rendererData.rig;
            rootBone = rig.bones[lastKandraRenderer.rendererData.rootBone];

            if (!anyRagdollFound) {
                for (int i = rootBone.childCount - 1; i >= 0; i--) {
                    var child = rootBone.GetChild(i);
                    if (child.gameObject.layer == RagdollsLayer) {
                        anyRagdollFound = true;
                        break;
                    }
                }
                if (!anyRagdollFound) {
                    rootBone.gameObject.layer = RagdollsLayer;
                    FixItSoonLog("No Ragdoll Found, setting rootBone to Ragdolls layer", rootBone.gameObject);
                }
            }
            
            if (!ignoreAlivePrefab) {
                PrepareAlivePrefab(animatorTransform, bounds!.Value);
            }

            PrepareKandraRenderers(kandraRenderers);
            PrepareHeadAndTorso(rootBone);
            if (isPrefabAsset) {
                prefabToPrepare = PrefabUtility.SaveAsPrefabAsset(prefabToModify, AssetDatabase.GetAssetPath(prefabToPrepare));
                PrefabUtility.UnloadPrefabContents(prefabToModify);
                DestroyImmediate(prefabToModify);
            }
            EditorUtility.SetDirty(prefabToPrepare);
            _result = true;
            head = null;
            torso = null;
            InfoLog("Finish Prepare");
        }

        void PrepareAnimatorBasedComponents(Animator animator) {
            var animatorGo = animator.gameObject;
            if (!animatorGo.TryGetComponent<ARNpcAnimancer>(out var arAnimator)) {
                animatorGo.AddComponent<ARNpcAnimancer>().Animator = animator;
            } else {
                arAnimator.Animator = animator;
            }

            if (!animatorGo.HasComponent<TG.Main.AI.Movement.RootMotions.RootMotion>()) {
                animatorGo.AddComponent<TG.Main.AI.Movement.RootMotions.RootMotion>();
            }
            
            if (!animatorGo.HasComponent<AnimatorClipPlayer>()) {
                animatorGo.AddComponent<AnimatorClipPlayer>();
            }
        }

        void PrepareAlivePrefab(Transform animatorTransform, Bounds bounds) {
            var localScale = bounds.extents;
            // Scale is modified because Cylinder mesh is 1x2x1
            localScale.x *= 2f;
            localScale.z *= 2f;
            
            var localPos = bounds.center;
            localPos.y = bounds.extents.y * 0.5f;

            var alivePrefab = new GameObject("AlivePrefab").transform;
            var walkThroughCollider = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            walkThroughCollider.gameObject.name = "WalkThroughCollider";

            alivePrefab.SetParent(animatorTransform);
            alivePrefab.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            alivePrefab.gameObject.tag = "AlivePrefab";
            walkThroughCollider.SetParent(alivePrefab);
            walkThroughCollider.SetLocalPositionAndRotation(localPos, Quaternion.identity);
            walkThroughCollider.localScale = localScale;
            walkThroughCollider.gameObject.layer = AIsLayer;
            DoubleCheckLog("WalkThroughCollider was set up from code, check it's size and position", walkThroughCollider.gameObject);

            var meshFilter = walkThroughCollider.GetComponent<MeshFilter>();
            var meshCollider = walkThroughCollider.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = true;
            
            DestroyImmediate(meshFilter);
            DestroyImmediate(walkThroughCollider.GetComponent<CapsuleCollider>());
            DestroyImmediate(walkThroughCollider.GetComponent<MeshRenderer>());
        }

        void PrepareKandraRenderers(List<KandraRenderer> renderers) {
            KandraRenderer vfxRendererBase = null;
            if (renderers.Count == 1) {
                vfxRendererBase = renderers[0];
            } else {
                RenderersMarkers markers = renderers[0].GetComponentInParent<RenderersMarkers>();
                var vfxRenderer = renderers.FirstOrDefault(r => r.gameObject.HasComponent<VFXBodyMarker>());
                var bodyRenderer = markers.KandraMarkers.FirstOrDefault(m => m.MaterialType.HasFlagFast(RendererMarkerMaterialType.Body)).Renderer;
                if (!bodyRenderer) {
                    DoubleCheckLog($"Found too many KandraRenderers, marking first one as Body: {renderers[0].gameObject.name}", renderers[0].gameObject);
                    bodyRenderer = renderers[0];
                    var kandraMarkers = markers.KandraMarkers;
                    Array.Resize(ref kandraMarkers, kandraMarkers.Length + 1);
                    kandraMarkers[^1] = new RenderersMarkers.KandraMarker(RendererMarkerMaterialType.Body, bodyRenderer, 0);

                }
                if (vfxRenderer == null) {
                    vfxRendererBase = bodyRenderer;
                }
            }

            if (vfxRendererBase == null) {
                return;
            }

            var parentTransform = vfxRendererBase.transform.parent;
            var newVfxRenderer = new GameObject("VFXRenderer");
            newVfxRenderer.transform.SetParent(parentTransform);
            newVfxRenderer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            newVfxRenderer.SetActive(false);
            var kandraRenderer = newVfxRenderer.AddComponent<KandraRenderer>();
            kandraRenderer.rendererData = vfxRendererBase.rendererData.Copy(kandraRenderer.gameObject);
            kandraRenderer.rendererData.rig = vfxRendererBase.rendererData.rig;
            if (vfxMesh != null) {
                kandraRenderer.rendererData.mesh = vfxMesh;
            } else {
                FixItSoonLog("VFX renderer was created. It requires Simplified Mesh!", newVfxRenderer);
            }
            newVfxRenderer.AddComponent<VFXBodyMarker>();
            newVfxRenderer.SetActive(true);
        }

        void PrepareHeadAndTorso(Transform rootBone) {
            rootBone.gameObject.tag = "RootBone";
            if (head == null) {
                head = new GameObject("Head").transform;
                head.SetParent(rootBone);
                head.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                FixItSoonLog("Head was not set up, created it under root bone.", head.gameObject);
            }
            head.gameObject.tag = "Head";

            if (torso == null) {
                torso = new GameObject("Torso").transform;
                torso.SetParent(rootBone);
                torso.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                FixItSoonLog("Torso was not set up, created it under root bone.", torso.gameObject);
            }
            torso.gameObject.tag = "Torso";
        }

        void FixItNowLog(string log, GameObject go = null) {
            log = $"[PrepareNPCPrefab] FIX IT NOW! {log} FIX IT NOW!";
            Log.Important?.Error(log, go);
            _logInfo += $"{log}\n";
        }
        
        void FixItSoonLog(string log, GameObject go = null) {
            log = $"[PrepareNPCPrefab] Please remember to fix it! {log}";
            Log.Important?.Error(log, go);
            _logInfo += $"{log}\n";
        }
        
        void DoubleCheckLog(string log, GameObject go = null) {
            log = $"[PrepareNPCPrefab] Double check! {log}";
            Log.Important?.Warning(log, go);
            _logInfo += $"{log}\n";
        }

        void InfoLog(string log) {
            log = $"[PrepareNPCPrefab] {log}";
            Log.Important?.Info(log);
            _logInfo += $"{log}\n";
        }
    }
}
// Adopted from https://www.youtube.com/watch?v=hcJ5luBs_jw
//with the exception of some minor additions, this is not the work of GameDevStudent. The script was downloaded from here: https://github.com/masterprompt/ModelStitching 

using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Kandra;
using Awaken.Kandra.VFXs;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.Blendshapes;
using Awaken.TG.Main.Utility.VFX;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using MagicaCloth2;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.VFX;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Heroes.SkinnedBones {
    public static class ClothStitcher {
        // === Cloths
        public static GameObject Stitch(GameObject assetObject, KandraRig baseRig) {
            var baseRigTransform = baseRig.transform;
            var bonesMap = baseRig.CreateBonesMap(0.5f, ARAlloc.Temp);
            var cullees = baseRig.GetComponentsInChildren<KandraTrisCullee>(true);
            var targetLayer = baseRig.gameObject.layer;

            var sourceClothes = assetObject.GetComponentsInChildren<KandraRenderer>();

            var mainParentName = "MainClothParent";
#if UNITY_EDITOR
            mainParentName = assetObject.name;
#endif
            var mainParent = new GameObject(mainParentName);
            mainParent.transform.SetParent(baseRigTransform, false);
            mainParent.layer = targetLayer;
            var mainParentTransform = mainParent.transform;
            mainParent.SetActive(false);

            var magicaColliders = baseRigTransform.GetComponentsInChildren<ColliderComponent>(true).ToList();

            var vfxs = assetObject.GetComponentsInChildren<VisualEffect>(true);
            var vfxCount = vfxs.Length;
            var vfxRedirects = vfxCount > 0 ? new Dictionary<KandraRenderer, KandraRenderer>(sourceClothes.Length) : null;

            for (int i = 0; i < sourceClothes.Length; i++) {
                var sourceCloth = sourceClothes[i];
                var clothName = "Cloth";
#if UNITY_EDITOR
                clothName = sourceCloth.name;
#endif
                GameObject clothGO;
                if (sourceClothes.Length == 1) {
                    clothGO = mainParent;
                    clothGO.name = clothName;
                } else {
                    clothGO = new GameObject(clothName);
                    clothGO.transform.SetParent(mainParentTransform, false);
                    clothGO.layer = targetLayer;
                }

                var realCloth = clothGO.AddComponent<KandraRenderer>();
                KandraRenderer.RedirectToRig(sourceCloth, realCloth, baseRig, ref bonesMap);

                var sourceClothTransform = sourceCloth.transform;

                HandleMeshCoverSettings(sourceClothTransform, clothGO);
                HandleDissolveAbleRenderer(sourceClothTransform, clothGO);
                HandleScalpMarkers(sourceClothTransform, clothGO);
                HandleCopyBlendShapesFromParent(sourceClothTransform, clothGO);
                HandleMagicaCloth(sourceClothTransform, clothGO, bonesMap, baseRig.bones, magicaColliders);
                HandleCulling(sourceClothTransform, cullees, clothGO);
                HandleMergedHair(assetObject, clothGO);

                if (vfxRedirects != null) {
                    vfxRedirects[sourceCloth] = realCloth;
                }

                sourceClothes[i] = realCloth;
            }

#if UNITY_EDITOR
            var particleSystem = assetObject.GetComponentInChildren<ParticleSystem>();
            if (particleSystem != null) {
                Log.Minor?.Error("Old particle system is not supported anymore, please use VFX instead", assetObject);
            }
#endif

            if (vfxCount > 0) {
                var sourceBinders = new List<VFXKandraRendererBinder>(4);
                var copyBinders = new List<VFXKandraRendererBinder>(4);
                for (int i = 0; i < vfxCount; i++) {
                    var visualEffect = vfxs[i];
                    var instance = Object.Instantiate(visualEffect.gameObject, mainParentTransform);
                    instance.layer = targetLayer;
                    visualEffect.GetComponents(sourceBinders);
                    if (sourceBinders.Count > 0) {
                        instance.GetComponents(copyBinders);
                        for (int j = 0; j < sourceBinders.Count; j++) {
                            var sourceBinder = sourceBinders[j];
                            if (sourceBinder.RequiresStitchingRebind == false) {
                                continue;
                            }
                            if (sourceBinder.kandraRenderer) {
                                if (vfxRedirects.TryGetValue(sourceBinder.kandraRenderer, out var realCloth)) {
                                    copyBinders[j].kandraRenderer = realCloth;
                                }
                            } else {
                                Log.Important?.Error($"There is no KandraRenderer binding on {sourceBinder.name} [{sourceBinder.GetType()}], VFX {visualEffect}[{visualEffect.visualEffectAsset}] won't work correctly", sourceBinder);
                            }
                        }
                        copyBinders.Clear();
                        sourceBinders.Clear();
                    }
                }
            }

            mainParent.SetActive(true);

            var stitchedClothes = sourceClothes;
            for (int i = 0; i < stitchedClothes.Length; i++) {
                stitchedClothes[i].EnsureInitialized();
            }

            bonesMap.Dispose();

            return mainParent;
        }

        /// <summary>
        /// Stitch clothing onto an avatar.  Both clothing and avatar must be instantiated however clothing may be destroyed after.
        /// </summary>
        /// <returns>Newly created clothing on avatar</returns>
        /// Asset: the object to add (usually clothing but also includes hairs etc.)
        /// Target: Existing avatar to add clothing to
        /// Result: The created clothing attached to target
        public static GameObject Stitch(GameObject assetObject, GameObject targetAvatar, TransformCatalog bonesCatalog = null, bool withBonesCatalogue = false) {
            bonesCatalog ??= new TransformCatalog(targetAvatar.transform);

            SkinnedMeshRenderer targetBody = targetAvatar.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (targetBody is null) {
                throw new Exception("[Critical] ClothStitcher: target avatar does not have any SkinnedMeshRenderer component!");
            }
            SkinnedMeshRenderer[] assetSkinnedMeshes = assetObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            List<ColliderComponent> magicaColliders = targetAvatar.GetComponentsInChildren<ColliderComponent>(true).ToList();
            GameObject targetClothing = AddChild(assetObject, targetAvatar.transform);

            List<SkinnedMeshRenderer> resultRenderers = new(assetSkinnedMeshes.Length);
            foreach (SkinnedMeshRenderer assetRenderer in assetSkinnedMeshes) {
                var assetTransform = assetRenderer.transform;
                var resultObject = AddChild(assetRenderer.gameObject, targetClothing.transform);
                SkinnedMeshRenderer resultRenderer = AddSkinnedMeshRenderer(assetRenderer, resultObject);
                resultRenderers.Add(resultRenderer);

                if (withBonesCatalogue) {
                    HandleAdditionalBones(bonesCatalog, assetRenderer, resultRenderer);
                }
                HandleMeshCoverSettings(assetTransform, resultObject);
                HandleDissolveAbleRenderer(assetTransform, resultObject);
                HandleScalpMarkers(assetTransform, resultObject);
                HandleRendererBones(bonesCatalog, resultRenderer, assetRenderer, targetBody, withBonesCatalogue);
                HandleMagicaCloth(bonesCatalog, assetRenderer, resultObject, magicaColliders);
                HandleCopyBlendShapesFromParent(assetTransform, resultObject);
                HandleMergedHair(assetObject, resultObject);

                resultRenderer.renderingLayerMask = assetRenderer.renderingLayerMask;
                bonesCatalog.ClearAdditional();
            }
            
            CopyParticles(targetClothing, assetObject, targetAvatar.transform, bonesCatalog);
            CopyVFXes(assetObject, targetClothing.transform, resultRenderers);

            return targetClothing;
        }

        static void HandleAdditionalBones(TransformCatalog bonesCatalog, SkinnedMeshRenderer assetRenderer, SkinnedMeshRenderer resultRenderer) {
            foreach (var additionalCatalog in assetRenderer.transform.parent.GetComponentsInChildren<AdditionalClothBonesCatalog>(true)) {
                additionalCatalog.CloneAndCatalog(bonesCatalog, out var rootInstance);
                BonesOwner bonesOwner = resultRenderer.gameObject.AddComponent<BonesOwner>();
                bonesOwner.rootBone = rootInstance;
            }
        }

        static void HandleRendererBones(TransformCatalog bonesCatalog, SkinnedMeshRenderer resultRenderer, SkinnedMeshRenderer assetRenderer, SkinnedMeshRenderer avatarBody, bool withBoneCatalogue) {
            resultRenderer.rootBone = avatarBody.rootBone;
            resultRenderer.localBounds = avatarBody.localBounds;
            resultRenderer.bones = withBoneCatalogue
                ? TranslateTransforms(assetRenderer.bones, bonesCatalog, resultRenderer)
                : TranslateTransformsWithoutBoneCatalogue(assetRenderer.bones, resultRenderer);
        }

        static void HandleMagicaCloth(TransformCatalog bonesCatalog, SkinnedMeshRenderer sourceRenderer, GameObject targetClothGameObject, List<ColliderComponent> magicaColliders) {
            MagicaCloth[] foundCloth = sourceRenderer.GetComponentsInParent<MagicaCloth>(true);
            MagicaCloth[] createdCloth = new MagicaCloth[foundCloth.Length];

            for (var index = 0; index < foundCloth.Length; index++) {
                createdCloth[index] = AddMagicaCloth(foundCloth[index], bonesCatalog, targetClothGameObject);
            }

            foreach (MagicaCloth cloth in createdCloth) {
                cloth.SerializeData.colliderCollisionConstraint.colliderList = magicaColliders;
                cloth.Initialize();
                cloth.BuildAndRun();
            }
        }

        static void HandleMeshCoverSettings(Transform sourceAssetTransform, GameObject resultObject) {
            MeshCoverSettings foundTagComponent = GameObjects.FindInAnyParent<MeshCoverSettings>(sourceAssetTransform, 3);
            if (foundTagComponent != null) {
                resultObject.AddComponent<MeshCoverSettings>().CopyFrom(foundTagComponent);
            }
        }
        
        static void HandleDissolveAbleRenderer(Transform sourceAssetTransform, GameObject resultObject) {
            DissolveAbleRenderer dissolveAbleRenderer = sourceAssetTransform.GetComponent<DissolveAbleRenderer>();
            if (dissolveAbleRenderer != null) {
                resultObject.AddComponent<DissolveAbleRenderer>().CopyFrom(dissolveAbleRenderer);
            }
        }

        static void HandleScalpMarkers(Transform sourceAssetTransform, GameObject resultObject) {
            ScalpMarker scalpMarker = sourceAssetTransform.GetComponent<ScalpMarker>();
            if (scalpMarker != null) {
                resultObject.AddComponent<ScalpMarker>();
            }
        }
        
        static void HandleCopyBlendShapesFromParent(Transform sourceAssetTransform, GameObject resultObject) {
            if (sourceAssetTransform.TryGetComponent(out CopyBlendshapesEditor _)) {
                resultObject.AddComponent<CopyBlendshapesRuntime>();
            }
        }

        static void HandleMagicaCloth(Transform sourceAssetTransform, GameObject targetClothGameObject, UnsafeHashMap<FixedString64Bytes, ushort> transformsMap, Transform[] rigTransforms, List<ColliderComponent> magicaColliders) {
            MagicaCloth[] foundCloth = sourceAssetTransform.GetComponentsInParent<MagicaCloth>(true);

            for (var index = 0; index < foundCloth.Length; index++) {
                var magicaCloth = AddMagicaCloth(foundCloth[index], transformsMap, rigTransforms, targetClothGameObject);
                if (magicaCloth != null) {
                    magicaCloth.SerializeData.colliderCollisionConstraint.colliderList = magicaColliders;
                    magicaCloth.Initialize();
                    magicaCloth.BuildAndRun();
                }
            }
        }

        static void HandleCulling(Transform sourceAssetTransform, KandraTrisCullee[] cullees, GameObject resultObject) {
            var sourceCuller = sourceAssetTransform.GetComponent<KandraTrisCuller>();

            if (sourceCuller) {
                var culler = resultObject.AddComponent<KandraTrisCuller>();
                culler.culledMeshes = sourceCuller.culledMeshes;

                foreach (var cullee in cullees) {
                    culler.Cull(cullee);
                }
            }
        }
        
        static void HandleMergedHair(GameObject assetObject, GameObject resultObject) {
            var hairController = assetObject.GetComponentInChildren<HairController>();
            if (hairController) {
                resultObject.AddComponent<HairController>();
            }
        }

        static GameObject AddChild(GameObject source, Transform parent) {
            GameObject target = new GameObject(source.name);
            target.transform.parent = parent;
            target.transform.localPosition = source.transform.localPosition;
            target.transform.localRotation = source.transform.localRotation;
            target.transform.localScale = source.transform.localScale;
            target.layer = parent.gameObject.layer;
            return target;
        }

        static SkinnedMeshRenderer AddSkinnedMeshRenderer(SkinnedMeshRenderer source, GameObject parent) {
            SkinnedMeshRenderer target = parent.AddComponent<SkinnedMeshRenderer>();
            target.sharedMesh = source.sharedMesh;
            target.sharedMaterials = source.sharedMaterials;
            target.shadowCastingMode = source.shadowCastingMode;
            return target;
        }

        static MagicaCloth AddMagicaCloth(MagicaCloth source, TransformCatalog transformCatalog, GameObject targetClothGameObject) {
            MagicaCloth target = targetClothGameObject.AddComponent<MagicaCloth>();
            target.SerializeData.Import(source.SerializeData, true);
            target.GetSerializeData2().selectionData = source.GetSerializeData2().selectionData;
            target.ReplaceTransform(transformCatalog);
            
            target.SerializeData.sourceRenderers = target.SerializeData.sourceRenderers
                                                         .WhereNotUnityNull()
                                                         .SelectWithLog(r => transformCatalog.TryGet(r.name)?.GetComponent<Renderer>(), "[ClothStitcher]", "Could not find transform or renderer in target transform", LogType.Minor)
                                                         .WhereNotNull()
                                                         .ToList();
            
            target.SerializeData.rootBones = target.SerializeData.rootBones
                                                   .WhereNotUnityNull()
                                                   .SelectWithLog(b => transformCatalog.TryGet(b.name), "[ClothStitcher]", "Could not find bone in transform catalogue", LogType.Minor)
                                                   .WhereNotNull()
                                                   .ToList();
            return target; 
        }

        static MagicaCloth AddMagicaCloth(MagicaCloth source, UnsafeHashMap<FixedString64Bytes, ushort> transformsMap, Transform[] rigTransforms, GameObject targetClothGameObject) {
            if (source.SerializeData.clothType == ClothProcess.ClothType.MeshCloth) {
                Log.Minor?.Error($"Trying to use MeshCloth but we don't support that type");
                return null;
            }
            MagicaCloth target = targetClothGameObject.AddComponent<MagicaCloth>();
            target.SerializeData.Import(source.SerializeData, true);
            target.GetSerializeData2().selectionData = source.GetSerializeData2().selectionData;
            target.ReplaceTransform(transformsMap, rigTransforms);

            return target;
        }

        static Transform[] TranslateTransforms(Transform[] assetBones, TransformCatalog transformCatalog, SkinnedMeshRenderer resultRenderer) {
            Transform[] targets = new Transform[assetBones.Length];

            for (int index = 0; index < assetBones.Length; index++) {
                Transform assetBone = assetBones[index];
                Transform targetBone = DictionaryExtensions.Find(transformCatalog, assetBone.name);
                
                if (targetBone is null) {
                    var additionalCatalog = assetBone.GetComponentInParent<AdditionalClothBonesCatalog>(true);
                    if (additionalCatalog != null) {
                        targetBone = DictionaryExtensions.Find(additionalCatalog.CurrentCatalog, assetBone.name);
                    }
                }
                
                // If we have targeted bone just assign
                if (targetBone != null) {
                    targets[index] = targetBone;
                } else {
                    Log.Minor?.Warning($"{resultRenderer.name} has additional bone but have no AdditionalClothBonesCatalog script ({assetBone.name})", resultRenderer);
                }
            }

            return targets;
        }

        static Transform[] TranslateTransformsWithoutBoneCatalogue(Transform[] assetBones, SkinnedMeshRenderer resultRenderer) {
            var bones = new Transform[assetBones.Length];
            var allBones = resultRenderer.transform.parent.parent.GetComponentsInChildren<Transform>();
            var allBonesNames = ArrayUtils.Select(allBones, bone => bone.name);
            var boneMapping = new Dictionary<Transform, Transform>();

            var needProcessing = false;
            for (int i = 0; i < bones.Length; i++) {
                var index = Array.IndexOf(allBonesNames, assetBones[i].name);
                if (index != -1) {
                    bones[i] = allBones[index];
                    boneMapping.Add(assetBones[i], bones[i]);
                } else {
                    needProcessing = true;
                }
            }

            while (needProcessing) {
                needProcessing = false;
                for (int i = 0; i < bones.Length; i++) {
                    if (bones[i] != null) {
                        continue;
                    }
                    if (boneMapping.TryGetValue(assetBones[i].parent, out var parent)) {
                        var bone = new GameObject(assetBones[i].name).transform;
                        bone.SetParent(parent);
                        assetBones[i].GetLocalPositionAndRotation(out var localPos, out var localRot);
                        bone.SetLocalPositionAndRotation(localPos, localRot);
                        bone.localScale = assetBones[i].localScale;
                        bones[i] = bone;
                        boneMapping.Add(assetBones[i], bones[i]);
                        needProcessing = true;
                    }
                }
            }
            
            return bones;
        }

        // === Others
        static void CopyParticles(GameObject currentCloth, GameObject source, Transform target, TransformCatalog bonesCatalog) {
            var particleSystems = source.GetComponentsInChildren<ParticleSystem>();
            var sourceTransform = source.transform;
            foreach (ParticleSystem particleSystem in particleSystems) {
                Transform targetBone = DictionaryExtensions.Find(bonesCatalog, particleSystem.transform.parent.name);
                if (targetBone != null) {
                    var instance = Object.Instantiate(particleSystem.gameObject, targetBone);
                    // make sure to cleanup
                    currentCloth.AddComponent<BonesOwner>().rootBone = instance.transform;
                } else if (particleSystem.transform.parent == sourceTransform) {
                    var instance = Object.Instantiate(particleSystem.gameObject, target);
                    // make sure to cleanup
                    currentCloth.AddComponent<BonesOwner>().rootBone = instance.transform;
                }
            }
        }
        
#pragma warning disable CS0618 // Type or member is obsolete
        static void CopyVFXes(GameObject source, Transform target, List<SkinnedMeshRenderer> newSkinnedMeshRenderers) {
            CopyVFXFromClothPrefab[] visualEffects = source.GetComponentsInChildren<CopyVFXFromClothPrefab>();
            foreach (CopyVFXFromClothPrefab visualEffect in visualEffects) {
                SkinnedMeshRenderer originalSkinnedMesh = visualEffect.GetComponent<VisualEffect>().GetSkinnedMeshRenderer(visualEffect.PropertyId);
                SkinnedMeshRenderer newSkinnedMeshRenderer = newSkinnedMeshRenderers.FirstOrDefault(s => s.name == originalSkinnedMesh.name);
                if (newSkinnedMeshRenderer == null) {
                    Log.Important?.Error("Failed to find corresponding SkinnedMeshRenderer for VFX", visualEffect);
                    continue;
                }
                GameObject instance = Object.Instantiate(visualEffect.gameObject, target);
                instance.GetComponent<CopyVFXFromClothPrefab>().SetSkinnedMeshRenderer(newSkinnedMeshRenderer);
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete
        
        // === TransformCatalog
        public class TransformCatalog : Dictionary<string, Transform> {
            List<string> additionalTransforms = new List<string>();
            // === Constructors
            public TransformCatalog(Transform transform) {
                Catalog(transform);
            }
            
            public TransformCatalog() {}

            // === Catalog
            public void Catalog(Transform transform) {
                if (transform.GetComponent<RuntimeAdditionalBonesRoot>() != null) {
                    return;
                }
                this[transform.name] = transform;

                foreach (Transform child in transform) {
                    Catalog(child);
                }
            }

            public void CatalogSingleAdditional(Transform transform) {
                if (!ContainsKey(transform.name)) {
                    additionalTransforms.Add(transform.name);
                    this[transform.name] = transform;
                }
            }

            public void ClearAdditional() {
                foreach (string additionalTransform in additionalTransforms) {
                    Remove(additionalTransform);
                }
                additionalTransforms.Clear();
            }
            
            public Transform TryGet(string name) => ContainsKey(name) ? this[name] : null;
        }
        
        // === DictionaryExtensions
        public static class DictionaryExtensions {
            public static TValue Find<TKey, TValue>(Dictionary<TKey, TValue> source, TKey key) {
                source.TryGetValue(key, out TValue value);
                return value;
            }
        }
    }
}
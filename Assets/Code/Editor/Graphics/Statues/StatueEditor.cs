using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.Editor.DrakeRenderer;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Graphics.Statues;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Heroes.SkinnedBones;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.Editor.MoreGUI;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Graphics.Statues {
    [CustomEditor(typeof(Statue))]
    public class StatueEditor : OdinEditor {
        public const string StatuesMeshesFolder = "Assets/3DAssets/Characters/Humans/Statues";
        bool bakeStaticModelAsInBuild;

        [InitializeOnLoadMethod]
        static void Initialize() {
            Statue.OnRegenerateEditableModelRequest -= RegenerateEditableModel;
            Statue.OnRegenerateEditableModelRequest += RegenerateEditableModel;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            var statueAccess = new Statue.EditorAccess((Statue)target);
            var changed = false;

            if (statueAccess.Clip) {
                var frame = EditorGUILayout.IntSlider(statueAccess.Frame, 0, statueAccess.Frames);
                if (statueAccess.Frame != frame) {
                    statueAccess.Frame = frame;
                    changed = true;
                }
            }

            var propsSelectors = statueAccess.Body.GetComponentsInChildren<StatuePropSelector>();
            if (statueAccess.Props.Length != propsSelectors.Length) {
                Array.Resize(ref statueAccess.Props, propsSelectors.Length);
                changed = true;
            }

            for (int i = 0; i < propsSelectors.Length; i++) {
                var selector = propsSelectors[i];
                selector.props.TryFind(prop => prop.guid == statueAccess.Props[i], out int index);
                index++;
                var newIndex = AREditorPopup.Draw(EditorGUILayout.GetControlRect(), GUIUtils.Content(selector.name), index,
                    () => {
                        var contents = new GUIContent[selector.props.Length + 1];
                        contents[0] = new GUIContent("(None)");
                        for (int j = 0; j < selector.props.Length; j++) {
                            contents[j + 1] = new GUIContent(selector.props[j].name);
                        }

                        return contents;
                    },
                    index => GUIUtils.Content(index == 0 ? "(None)" : selector.props[index - 1].name)
                );
                if (newIndex != index) {
                    statueAccess.Props[i] = newIndex == 0 ? default : selector.props[newIndex - 1].guid;
                    changed = true;
                }
            }

            if (changed) {
                EditorUtility.SetDirty(statueAccess.Statue);
                RegenerateEditableModel(statueAccess);
            }

            if (GUILayout.Button("Regenerate editable model")) {
                RegenerateEditableModel(statueAccess);
            }

            GUILayout.BeginHorizontal();
            bakeStaticModelAsInBuild = GUILayout.Toggle(bakeStaticModelAsInBuild, "Build Mode");
            if (GUILayout.Button("Regenerate static model")) {
                var go = RegenerateStaticModel(statueAccess, bakeStaticModelAsInBuild);
                SetGameObjectAndChildsHideFlags(go.transform, HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable);
            }

            GUILayout.EndHorizontal();
        }

        public static GameObject RegenerateStaticModel(Statue.EditorAccess statueAccess, bool modelForBuild, float screenRelativeTransitionHeight = -1) {
            ref var staticInstance = ref statueAccess.StaticInstance;
            Statue.TryDestroyStaticInEditorInstance(staticInstance);
            screenRelativeTransitionHeight = screenRelativeTransitionHeight < 0 ? statueAccess.ScreenRelativeTransitionHeight : screenRelativeTransitionHeight;
            var meshPath = modelForBuild ? $"{StatuesMeshesFolder}/{statueAccess.RootTransform?.name}.mesh" : string.Empty;
            staticInstance = GenerateStaticModel(statueAccess, screenRelativeTransitionHeight, meshPath);
            if (staticInstance == null) {
                return new GameObject("NotValidStaticStatueModel");
            }
            if (modelForBuild) {
                var meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                AddressableHelper.AddEntry(new AddressableEntryDraft.Builder(meshAsset)
                    .InGroup(AddressableGroup.DrakeRenderer)
                    .WithAddressProvider(static (obj, _) => obj.name).Build());

                var drakeToBake = staticInstance.AddComponent<DrakeToBake>();
                DrakeEditorHelpers.Bake(drakeToBake);
            }
            return staticInstance;
        }

        static void RegenerateEditableModel(Statue statue) => RegenerateEditableModel(new Statue.EditorAccess(statue));

        static void RegenerateEditableModel(Statue.EditorAccess statueAccess) {
            if (statueAccess.EditableInstance != null) {
                DestroyImmediate(statueAccess.EditableInstance);
            }

            if (statueAccess.StaticInstance != null) {
                DestroyImmediate(statueAccess.StaticInstance);
            }

            statueAccess.EditableInstance = CreateEditableModel(statueAccess);
            if (statueAccess.EditableInstance == null) {
                return;
            }

            statueAccess.EditableInstance.transform.SetParent(statueAccess.Statue.transform);
            statueAccess.EditableInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            statueAccess.EditableInstance.transform.localScale = Vector3.one;
            foreach (var t in statueAccess.EditableInstance.GetComponentsInChildren<Transform>(true)) {
                t.gameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
            }
        }

        static void ClearEditableModel(Statue.EditorAccess statueAccess) {
            if (statueAccess.EditableInstance == null) {
                return;
            }

            GameObjects.DestroySafely(statueAccess.EditableInstance);
            statueAccess.EditableInstance = null;
        }

        static GameObject CreateEditableModel(Statue.EditorAccess statueAccess) {
            if (statueAccess.Body == null) {
                return null;
            }

            GameObject go = null;
            try {
                go = Instantiate(statueAccess.Body);
                SetEditabeModelProps(statueAccess, go);
                SetEditableModePose(statueAccess, go);
                StitchEditableModelClothes(statueAccess, go);
                SetEditableModelMaterial(statueAccess, go);
                return go;
            } catch (Exception e) {
                Debug.LogException(e, statueAccess.Statue);
                DestroyImmediate(go);
                return null;
            }
        }

        static void SetEditabeModelProps(Statue.EditorAccess statueAccess, GameObject go) {
            var propSelectors = go.GetComponentsInChildren<StatuePropSelector>();
            int propCount = math.min(propSelectors.Length, statueAccess.Props.Length);
            for (int i = 0; i < propCount; i++) {
                propSelectors[i].Set(statueAccess.Props[i]);
            }
        }

        static void SetEditableModePose(Statue.EditorAccess statueAccess, GameObject go) {
            var animationRoot = go;
            var animator = go.GetComponentInChildren<Animator>();
            if (animator) {
                animationRoot = animator.gameObject;
                var rigBuilder = animator.GetComponent<RigBuilder>();
                if (rigBuilder) {
                    DestroyImmediate(rigBuilder);
                }

                DestroyImmediate(animator);
            }

            if (statueAccess.Clip) {
                statueAccess.Clip.SampleAnimation(animationRoot, statueAccess.Frame / statueAccess.Clip.frameRate);
            }
        }

        static void StitchEditableModelClothes(Statue.EditorAccess statueAccess, GameObject go) {
            var baseRig = go.GetComponentInChildren<KandraRig>();
            for (int i = 0; i < statueAccess.Parts.Length; i++) {
                if (statueAccess.Parts[i]) {
                    ClothStitcher.Stitch(statueAccess.Parts[i], baseRig);
                }
            }

            foreach (var hairController in go.GetComponentsInChildren<HairController>()) {
                DestroyImmediate(hairController);
            }
        }

        static void SetEditableModelMaterial(Statue.EditorAccess statueAccess, GameObject go) {
            foreach (var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                renderer.sharedMaterials = ArrayUtils.Repeat(statueAccess.Material, math.min(renderer.sharedMaterials.Length, renderer.sharedMesh.subMeshCount));
            }

            foreach (var renderer in go.GetComponentsInChildren<KandraRenderer>()) {
                var materialsCount = renderer.rendererData.MaterialsCount;
                var submeshesCount = renderer.rendererData.mesh.submeshes.Length;
                renderer.ChangeOriginalMaterials(ArrayUtils.Repeat(statueAccess.Material, math.min(materialsCount, submeshesCount)));
            }

            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>()) {
                var filter = renderer.GetComponent<MeshFilter>();
                renderer.sharedMaterials = ArrayUtils.Repeat(statueAccess.Material, math.min(renderer.sharedMaterials.Length, filter.sharedMesh.subMeshCount));
            }
        }

        static List<GameObject> CreateCollidersHierarchyCopy(GameObject root) {
            var gameObjectsWithColliders = new Dictionary<GameObject, List<Collider>>();

            var allColliders = root.GetComponentsInChildren<Collider>();
            foreach (var collider in allColliders) {
                if (gameObjectsWithColliders.TryGetValue(collider.gameObject, out var gameObjectColliders) == false) {
                    gameObjectColliders = new List<Collider>(2);
                    gameObjectsWithColliders.Add(collider.gameObject, gameObjectColliders);
                }
                gameObjectColliders.Add(collider);
            }
            var gameObjectsWithCollidersCopy = new List<GameObject>(gameObjectsWithColliders.Count);
            root.transform.GetPositionAndRotation(out var rootPos, out var rootRot);
            foreach (var (gameObject, colliders) in gameObjectsWithColliders) {
                var collidersPartGO = new GameObject(gameObject.name);
                gameObject.transform.GetPositionAndRotation(out var pos, out var rot);
                collidersPartGO.transform.SetPositionAndRotation(pos, rot);
                collidersPartGO.transform.localScale = gameObject.transform.lossyScale;
                foreach (var collider in colliders) {
                    var colliderCopy = collidersPartGO.AddComponent(collider.GetType());
                    EditorUtility.CopySerialized(collider, colliderCopy);
                    EditorUtility.SetDirty(colliderCopy);
                }
                gameObjectsWithCollidersCopy.Add(collidersPartGO);
            }
            return gameObjectsWithCollidersCopy;
        }

        static GameObject GenerateStaticModel(Statue.EditorAccess statueAccess, float screenRelativeTransitionHeight, string meshPath) {
            var statue = statueAccess.Statue;
            RegenerateEditableModel(statueAccess);
            var (combinedMesh, root) = CombineMeshes(statueAccess);
            var gameObjectsWithColliders = CreateCollidersHierarchyCopy(statueAccess.EditableInstance);
            ClearEditableModel(statueAccess);
            var statueRoot = statueAccess.RootTransform;
            if (statueRoot == null) {
                Statue.LogStatueShouldHaveStaticRootParent(statue);
                return null;
            }

            var statueRootTransform = statueRoot.transform;
            var statueName = statueRootTransform.name;
            combinedMesh.name = statueName;

            if (string.IsNullOrEmpty(meshPath) == false) {
                var combinedMeshPath = SaveMeshToAssetDatabaseAndDispose(combinedMesh, meshPath);
                combinedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(combinedMeshPath);
            }

            var rootGameObject = new GameObject($"StatueStaticModel_{statueName}");
            rootGameObject.transform.SetParent(root);
            rootGameObject.transform.localPosition = Vector3.zero;
            rootGameObject.transform.localRotation = Quaternion.identity;
            rootGameObject.transform.localScale = Vector3.one;
            rootGameObject.isStatic = true;

            var rendererGameObject = new GameObject();
            rendererGameObject.transform.SetParent(rootGameObject.transform);
            rendererGameObject.transform.localPosition = Vector3.zero;
            rendererGameObject.transform.localRotation = Quaternion.identity;
            rendererGameObject.transform.localScale = Vector3.one;
            rendererGameObject.isStatic = true;

            var staticMeshRenderer = rendererGameObject.AddComponent<MeshRenderer>();
            staticMeshRenderer.sharedMaterial = statueAccess.Material;
            var staticMeshFilter = rendererGameObject.AddComponent<MeshFilter>();
            staticMeshFilter.sharedMesh = combinedMesh;

            LODGroup staticLodGroup = rootGameObject.AddComponent<LODGroup>();
            staticLodGroup.SetLODs(new[] {
                new LOD() {
                    screenRelativeTransitionHeight = screenRelativeTransitionHeight,
                    renderers = new Renderer[] { staticMeshRenderer }
                }
            });
            foreach (var gameObjectWithCollider in gameObjectsWithColliders) {
                gameObjectWithCollider.transform.SetParent(rootGameObject.transform, true);
            }
            return rootGameObject;
        }

        static string SaveMeshToAssetDatabaseAndDispose(Mesh mesh, string meshPath) {
            var existingAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingAsset != null) {
                existingAsset.Clear();
                EditorUtility.CopySerialized(mesh, existingAsset);
                DestroyImmediate(mesh);
            } else {
                // When creating mesh asset, mesh somehow starts pointing to created mesh asset, so it shouldn't be destroyed 
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            AssetDatabase.SaveAssets();
            return meshPath;
        }

        static (Mesh mesh, Transform root) CombineMeshes(Statue.EditorAccess statueAccess) {
            var combinedMesh = new Mesh() {
                indexFormat = IndexFormat.UInt32
            };
            var statueRootTransform = statueAccess.Statue.transform.parent;
            var parentWorldToLocal = statueRootTransform.worldToLocalMatrix;

            var statueVisualsRootTransform = statueAccess.EditableInstance.transform;
            var kandraRenderers = statueVisualsRootTransform.GetComponentsInChildren<KandraRenderer>();
            var drakeLods = statueVisualsRootTransform.GetComponentsInChildren<DrakeLodGroup>();
            var kandraRenderersCount = kandraRenderers.Length;

            var combinedInstances = new List<CombineInstance>(kandraRenderersCount + drakeLods.Length);
            for (int i = 0; i < kandraRenderersCount; i++) {
                var kandraRenderer = kandraRenderers[i];
                var mesh = kandraRenderer.BakePoseMesh(kandraRenderer.transform.worldToLocalMatrix.Orthonormal());
                Matrix4x4 childMatrixInParentSpace = parentWorldToLocal * kandraRenderer.transform.localToWorldMatrix;
                int subMeshCount = mesh.subMeshCount;
                for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++) {
                    combinedInstances.Add(new CombineInstance() {
                        mesh = mesh,
                        transform = childMatrixInParentSpace,
                        subMeshIndex = subMeshIndex
                    });
                }
            }

            for (int i = 0; i < drakeLods.Length; i++) {
                var drakeLod = drakeLods[i];
                if (IsHiddenComponent(drakeLod)) {
                    continue;
                }

                foreach (var renderer in drakeLod.Renderers) {
                    if (renderer == null || (renderer.LodMask & 1) != 1) {
                        continue;
                    }

                    Mesh mesh;
                    try {
                        mesh = DrakeEditorHelpers.LoadAsset<Mesh>(renderer.MeshReference);
                    } catch (Exception e) {
                        Log.Important?.Error($"Exception while processing drake renderer {renderer.name}", renderer);
                        Debug.LogException(e);
                        continue;
                    }

                    Matrix4x4 childMatrixInParentSpace = parentWorldToLocal * (Matrix4x4)renderer.LocalToWorld;
                    var subMeshCount = mesh.subMeshCount;
                    for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++) {
                        combinedInstances.Add(new CombineInstance() {
                            mesh = mesh,
                            transform = childMatrixInParentSpace,
                            subMeshIndex = subMeshIndex
                        });
                    }
                }
            }

            combinedMesh.CombineMeshes(combinedInstances.ToArray(), true, true);
            for (int i = 0; i < kandraRenderersCount; i++) {
                DestroyImmediate(combinedInstances[i].mesh);
            }

            combinedMesh.colors = null;
            combinedMesh.UploadMeshData(true);
            return (combinedMesh, statueRootTransform);

            static bool IsHiddenComponent(Component c) {
                return ((c.hideFlags & HideFlags.HideInInspector) == 0 && ((c.transform.parent == null) || ((c.transform.parent.gameObject.hideFlags & HideFlags.HideInInspector) == 0))) == false;
            }
        }

        static void SetGameObjectAndChildsHideFlags(Transform transform, HideFlags hideFlags) {
            transform.gameObject.hideFlags = hideFlags;
            for (int i = 0; i < transform.childCount; i++) {
                SetGameObjectAndChildsHideFlags(transform.GetChild(i), hideFlags);
            }
        }
    }
}
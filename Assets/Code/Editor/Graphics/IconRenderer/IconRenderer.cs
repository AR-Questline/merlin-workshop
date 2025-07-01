using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.ECS.Editor.DrakeRenderer;
using Awaken.TG.Assets;
using Awaken.TG.Debugging;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Main.Heroes.Items;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Graphics.IconRenderer {
    public static class IconRenderer {
        const string RenderingScene = "Assets/Scenes/Dev_Scenes/IconRenderingScene.unity";

        static IconRendererSettings s_settings;
        static ItemRendererReferences s_references;

        public static IconRendererSettings Settings => s_settings ??= AssetDatabase.LoadAssetAtPath<IconRendererSettings>("Assets/2DAssets/RawRenderedIcons/IconRenderingSettings.asset");
        
        static ItemRendererReferences References => s_references = s_references ? s_references : Object.FindAnyObjectByType<ItemRendererReferences>();
        static Camera RenderCamera => References != null ? References.renderCamera : null;
        static Transform RenderObjectParent => References != null ? References.renderObjectParent : null;
        static RenderTexture RenderTexture => Settings.renderTexture;
        static Scene Scene { get; set; }
        static IconRenderingSettings CurrentPreview { get; set; }
        static Dictionary<int, string> GeneratedIcons { get; } = new();

        public static void PreviewItem(IconRenderingSettings iconRenderingSettings, TransformValues? transformValues,
            SerializableDictionary<string, TransformValues> rigTransforms = null) {
            CurrentPreview = iconRenderingSettings;
            TryOpenRenderingScene(() => {
                ClearRenderObjectParent();
                if (transformValues != null) {
                    Transform(transformValues.Value, iconRenderingSettings);
                }

                var propProvider = iconRenderingSettings.prefab.GetComponent<IIconized>();
                if (propProvider == null) {
                    Log.Important?.Error($"IconRenderer: {iconRenderingSettings.prefab} does not implement IIconRendererPropProvider!");
                    return;
                }

                GameObject instanceMesh = propProvider.InstantiateProp(RenderObjectParent);

                if (rigTransforms != null) {
                    SetRig(rigTransforms);
                }

                if (instanceMesh == null) {
                    Log.Important?.Error($"IconRenderer: Couldn't instantiate {iconRenderingSettings.prefab.name} mesh");
                }
            });
        }

        public static void RenderIcons() {
            IconRenderer.TryOpenRenderingScene(() => EditorCoroutineUtility.StartCoroutine(IconRenderer.RenderIconsRoutine(), Settings));
        }

        public static void RenderIcon(IconRendererCategory entry, IconRenderingSettings renderingSettings) {
            GeneratedIcons.Clear();
            IconRenderer.TryOpenRenderingScene(() => EditorCoroutineUtility.StartCoroutine(HandleSingularIconCreation(entry, renderingSettings), Settings));
        }

        static IEnumerator RenderIconsRoutine() {
            GeneratedIcons.Clear();
            int count = Settings.categories.Sum(x => x.IconsRenderingSettings.Count);
            int sum = 0;
            var progressBar = ProgressBar.Create("Rendering icons", null, true);
            foreach (var category in Settings.categories.Where(entry => entry.use)) {
                for (int i = 0; i < category.IconsRenderingSettings.Count; i++) {
                    var currentIconRenderingSettings = category.IconsRenderingSettings[i];
                    GameObject prefab = currentIconRenderingSettings.prefab;
                    if (prefab == null) {
                        continue;
                    }

                    if (progressBar.DisplayCancellable((float)(i + sum) / count, prefab.name)) {
                        yield break;
                    }

                    yield return HandleSingularIconCreation(category, currentIconRenderingSettings);
                }

                sum += category.IconsRenderingSettings.Count;
            }

            ClearRenderObjectParent();
            Settings.UseNone();
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        static IEnumerator HandleSingularIconCreation(IconRendererCategory category, IconRenderingSettings iconRenderingSettings) {
            bool isDrakeHacked = DrakeHackToolbarButton.SceneAuthoringHack;
            DrakeHackToolbarButton.SceneAuthoringHack = true;
            ClearRenderObjectParent();
            var prefab = iconRenderingSettings.prefab;
            var propProvider = prefab.GetComponent<IIconized>();
            if (propProvider == null) {
                Log.Important?.Error($"IconRenderer: {prefab} does not implement IIconRendererPropProvider!");
                DrakeHackToolbarButton.SceneAuthoringHack = isDrakeHacked;
                yield break;
            }

            int prefabInstanceID = prefab.GetInstanceID();
            if (GeneratedIcons != null && GeneratedIcons.TryGetValue(prefab.GetInstanceID(), out string path)) {
                TryAssignIconToTemplate(prefab, path);
                DrakeHackToolbarButton.SceneAuthoringHack = isDrakeHacked;
                yield break;
            }

            GameObject instanceMesh = propProvider.InstantiateProp(RenderObjectParent);

            if (instanceMesh == null) {
                Log.Important?.Error($"Couldn't instantiate {prefab.name} mesh.", prefab);
                DrakeHackToolbarButton.SceneAuthoringHack = isDrakeHacked;
                yield break;
            }

            Transform(category.transform, iconRenderingSettings);
            yield return null;
            if (category.fitToCamera) {
                FitMesh(instanceMesh);
            }

            if (category.rigTransform) {
                SetRig(category.RigTransforms);
            }

            yield return new EditorWaitForSeconds(1f);
            RenderIcon(instanceMesh.name, out string iconPath);
            if (TryAssignIconToTemplate(prefab, iconPath)) {
                GeneratedIcons?.Add(prefabInstanceID, iconPath);
            }

            DrakeHackToolbarButton.SceneAuthoringHack = isDrakeHacked;
        }

        public static void Transform(TransformValues transform, IconRenderingSettings iconRenderingSettings = null) {
            iconRenderingSettings ??= CurrentPreview;
            RenderObjectParent.ApplyValues(transform + iconRenderingSettings.customTransformOffset);
        }

        static void SetRig(SerializableDictionary<string, TransformValues> rigTransforms) {
            var bones = GetBonesFromScene();
            if (bones == null) {
                Log.Important?.Error("IconRenderer rig setup failed: Root not found");
                return;
            }

            foreach (var bone in bones) {
                if (rigTransforms.TryGetValue(bone.name, out var values)) {
                    bone.ApplyValues(values);
                }
            }
        }

        static void TryOpenRenderingScene(Action sceneOpenCallback) {
            if (sceneOpenCallback == null) {
                throw new ArgumentNullException();
            }

            if (SceneManager.GetActiveScene().handle == Scene.handle) {
                sceneOpenCallback();
            } else {
                string message = "Previewing or rendering icons requires IconRenderingScene to be opened.\n" +
                                 "Make sure your changes are saved before proceeding.";
                if (EditorUtility.DisplayDialog("Icon Renderer", message, "Proceed", "Cancel")) {
                    EditorCoroutineUtility.StartCoroutine(OpenSceneRoutine(sceneOpenCallback), Settings);
                }
            }
        }

        static IEnumerator OpenSceneRoutine(Action sceneOpenCallback) {
            Scene = EditorSceneManager.OpenScene(RenderingScene, OpenSceneMode.Single);
            yield return new WaitUntil(() => Scene.isLoaded);
            sceneOpenCallback();
        }

        public static IEnumerable<Transform> GetBonesFromScene() =>
            RenderObjectParent.GetComponentInChildren<SkinnedMeshRenderer>()?.rootBone.GetComponentsInChildren<Transform>();

        static bool TryAssignIconToTemplate(GameObject prefab, string iconPath) {
            IIconized iconized = prefab.GetComponent<IIconized>();
            if (iconized == null) {
                Log.Important?.Error($"IconRenderer: {prefab} does not implement IIconRendererPropProvider!");
                return false;
            }

            AssetDatabase.ImportAsset(iconPath);
            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer == null) {
                Log.Important?.Error($"IconRenderer: Couldn't import icon ({iconPath}) with {nameof(TextureImporter)}");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedLQ;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite == null) {
                var obj = AssetDatabase.LoadMainAssetAtPath(iconPath);
                Log.Important?.Error($"IconRenderer: Couldn't load icon ({iconPath}) or load it as Sprite", obj);
                return false;
            }

            string guid = AddressableHelper
                .AddEntry(new AddressableEntryDraft.Builder(sprite)
                    .InGroup("ItemsIcons")
                    .WithAddressProvider((_, a) => ItemTemplateEditor.GetIconAddressName(a.MainAsset))
                    .WithLabels(ItemTemplateEditor.Labels)
                    .Build());

            iconized.SetIconReference(new ShareableSpriteReference(guid));
            EditorUtility.SetDirty(prefab);

            return true;
        }

        static void FitMesh(GameObject mesh) {
            RenderCamera.aspect = (float)RenderTexture.width / RenderTexture.height;
            var cameraPosition = RenderCamera.transform.position;
            Bounds bounds = TransformBoundsUtil.FindBounds(mesh.transform, false);
            mesh.transform.position = RenderObjectParent.position - bounds.center;
            cameraPosition.x = RenderObjectParent.position.x;
            cameraPosition.y = RenderObjectParent.position.y;
            RenderCamera.transform.position = cameraPosition;
            bounds = TransformBoundsUtil.FindBounds(mesh.transform, false);
            RenderCamera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y);
        }

        static void RenderIcon(string fileName, out string iconPath) {
            RenderCamera.targetTexture = RenderTexture;
            RenderCamera.Render();
            RenderTexture.active = RenderTexture;
            Texture2D texture2D = new(RenderTexture.width, RenderTexture.height);
            texture2D.Apply(false);
            texture2D.alphaIsTransparency = true;
            texture2D.ReadPixels(new Rect(0, 0, RenderCamera.pixelWidth, RenderCamera.pixelHeight), 0, 0);
            RenderTexture.active = null;
            RenderCamera.targetTexture = null;
            RenderTexture.DiscardContents();
            byte[] bytes = texture2D.EncodeToPNG();
            iconPath = $"{Settings.outputPath}/{fileName}_icon.png";
            File.WriteAllBytes(iconPath, bytes);
        }

        static void ClearRenderObjectParent() {
            if (!RenderObjectParent) {
                return;
            }

            foreach (Transform child in RenderObjectParent.transform) {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        public static TransformValues? GetRenderObjectParentTransformValues() {
            if (RenderObjectParent == null) {
                return null;
            }

            return new TransformValues(RenderObjectParent);
        }
    }
}
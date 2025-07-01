using System;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Saving.LargeFiles;
using Awaken.TG.MVC;
using UnityEngine;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Heroes.Sketching {
    public static class HeroSketches {
        const int RenderTextureDepth = 24;

        public static async UniTask<Texture2D> TakeScreenshotSketch(Transform heroRootTransform) {
            var mainCamera = Camera.main;
            if (mainCamera == null) {
                Log.Important?.Error("No main camera");
                return CreateDummyTexture();
            }

            var sketchVolumeReference = GameConstants.Get.SketchVolumePrefabReference.Get();
            if (sketchVolumeReference == null) {
                Log.Important?.Error("Sketch volume reference is null");
                return CreateDummyTexture();
            }
            sketchVolumeReference.ReleaseAsset();

            var loadSketchHandle = sketchVolumeReference.LoadAsset<GameObject>();

            var cameraCopyGO = new GameObject("SketchCamera");
            cameraCopyGO.SetActive(false);

            var cameraCopy = cameraCopyGO.AddComponent<Camera>();
            var cameraCopyHdAdditionalData = cameraCopyGO.AddComponent<HDAdditionalCameraData>();

            cameraCopyGO.transform.CopyPositionAndRotationFrom(mainCamera.transform);
            cameraCopy.CopyFrom(mainCamera);
            mainCamera.GetComponent<HDAdditionalCameraData>().CopyTo(cameraCopyHdAdditionalData);

            cameraCopyHdAdditionalData.allowDynamicResolution = false;

            var handsRenderersLayer = GameConstants.Get.SketchHandsRenderersTempLayer;
            MoveHandRenderersToHiddenForCameraCopyLayer(heroRootTransform, handsRenderersLayer, out var disabledRenderers, out var disabledKandraRenderers,
                out var prevLayers);

            // Make so that mainCamera would render hands and items but cameraCopy would not
            var mainCameraPrevCullingMask = mainCamera.cullingMask;
            var handsRenderersLayerMask = 1 << handsRenderersLayer;
            cameraCopy.cullingMask &= (~handsRenderersLayerMask);
            mainCamera.cullingMask |= (handsRenderersLayerMask);

            var screenshotTexture = new RenderTexture(Sketch.Width, Sketch.Height, RenderTextureDepth);
            cameraCopy.targetTexture = screenshotTexture;

            cameraCopyGO.SetActive(true);
            var gameConstants = GameConstants.Get;
            await UniTask.DelayFrame(gameConstants.NewCameraStabilisationDelayFramesCount);

            var result = await loadSketchHandle;
            if (result == null) {
                loadSketchHandle.Release();
                Log.Important?.Error("Sketch volume instance is null");
                return CreateDummyTexture();
            }

            GameObject sketchVolumeInstance = Object.Instantiate(result);
            sketchVolumeInstance.SetActive(true);
            await UniTask.DelayFrame(gameConstants.SketchVolumeStabilisationDelayFramesCount);

            // Make sketchVolume affect cameraCopy
            var sketchVolumeMask = 1 << sketchVolumeInstance.layer;
            cameraCopyHdAdditionalData.volumeLayerMask.value |= sketchVolumeMask;
            await UniTask.DelayFrame(gameConstants.SketchingExposureStabilisationDelayFramesCount);

            var sketchTexture = CreateScreenshot(cameraCopy, Sketch.Width, Sketch.Height);

            Object.Destroy(cameraCopyGO);
            Object.Destroy(screenshotTexture);
            Object.Destroy(sketchVolumeInstance);
            loadSketchHandle.Release();

            ResetRenderersLayers(disabledRenderers, disabledKandraRenderers, prevLayers);
            mainCamera.cullingMask = mainCameraPrevCullingMask;

            return sketchTexture;
        }

        static Texture2D CreateScreenshot(Camera camera, int width, int height) {
            try {
                var screenshotRT = camera.targetTexture;
                camera.Render();
                var screenshotTexture = new Texture2D(width, height, TextureFormat.RGB24, 1, false, true);
                var prevActiveRenderTexture = RenderTexture.active;
                RenderTexture.active = screenshotRT;
                screenshotTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshotTexture.Apply();
                RenderTexture.active = prevActiveRenderTexture;
                return screenshotTexture;
            } catch (Exception e) {
                Log.Critical?.Error("Failed to create sketch screenshot with exception:");
                Debug.LogException(e);
                return CreateDummyTexture();
            }
        }

        public static Texture2D LoadSketch(int index) {
            World.Services.Get<LargeFilesStorage>().TryLoadFile(index, out Texture2D texture);
            return texture;
        }

        static void MoveHandRenderersToHiddenForCameraCopyLayer(Transform renderersToHideRoot, int noRenderLayer,
            out Renderer[] disabledRenderers, out KandraRenderer[] disabledKandraRenderers, out int[] prevLayers) {
            disabledRenderers = renderersToHideRoot.GetComponentsInChildren<Renderer>();
            int handRenderersCount = disabledRenderers.Length;
            disabledKandraRenderers = renderersToHideRoot.GetComponentsInChildren<KandraRenderer>();
            int handKandraRenderersCount = disabledKandraRenderers.Length;

            prevLayers = new int[handRenderersCount + handKandraRenderersCount];
            for (int rendererIndex = 0; rendererIndex < handRenderersCount; rendererIndex++) {
                var renderer = disabledRenderers[rendererIndex];
                prevLayers[rendererIndex] = renderer.gameObject.layer;
                renderer.gameObject.layer = noRenderLayer;
            }

            for (int rendererIndex = 0; rendererIndex < handKandraRenderersCount; rendererIndex++) {
                var renderer = disabledKandraRenderers[rendererIndex];
                prevLayers[handRenderersCount + rendererIndex] = renderer.gameObject.layer;
                renderer.gameObject.layer = noRenderLayer;
                renderer.RefreshFilterSettings();
            }
        }

        static void ResetRenderersLayers(Renderer[] disabledRenderers, KandraRenderer[] disabledKandraRenderers, int[] prevLayers) {
            var disabledRenderersCount = disabledRenderers.Length;
            for (int i = 0; i < disabledRenderersCount; i++) {
                disabledRenderers[i].gameObject.layer = prevLayers[i];
            }

            var disabledKandraRenderersCount = disabledKandraRenderers.Length;
            for (int i = 0; i < disabledKandraRenderersCount; i++) {
                var renderer = disabledKandraRenderers[i];
                renderer.gameObject.layer = prevLayers[disabledRenderersCount + i];
                renderer.RefreshFilterSettings();
            }
        }

        static Texture2D CreateDummyTexture() {
            var blackTexture = Texture2D.blackTexture;
            var dummyTexture = new Texture2D(blackTexture.width, blackTexture.height, TextureFormat.RGB24, 1, false, true);
            dummyTexture.SetPixels(blackTexture.GetPixels());
            dummyTexture.Apply();
            return dummyTexture;
        }
    }
}
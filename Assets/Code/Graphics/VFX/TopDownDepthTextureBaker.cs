using UnityEngine;
#if UNITY_EDITOR
using System;
using System.IO;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Files;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Awaken.ECS.DrakeRenderer.Systems;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Awaken.TG.Graphics.VFX {
    public class TopDownDepthTextureBaker : MonoBehaviour {
#if UNITY_EDITOR
        const string DrakeRendererHighestLodModeName = "DrakeRenderer.HighestLodMode";
        static readonly int ConstantsPropId = Shader.PropertyToID("Constants");
        static readonly int DepthMapPropId = Shader.PropertyToID("DepthMap");
        static readonly int MaskPropId = Shader.PropertyToID("Mask");
        static readonly int FinalTexturePropId = Shader.PropertyToID("FinalTexture");
        static readonly string SetMaskKernelName = "SetMaskTexture";
        static readonly string InitializeMaskKernelName = "InitializeMaskTexture";

        static readonly string SmoothAndWriteToFinalTextureKernelName = "SmoothAndWriteToFinalTexture";

        [SerializeField, Required] GroundBounds groundBounds;
        [SerializeField] LayerMask cullingMask;
        [SerializeField] ComputeShader postProcessingShader;
        [ShowInInspector] bool previewChunks;
        [ShowInInspector, ShowIf(nameof(previewChunks))] int fontSize = 10;

        DepthCustomPass _depthCustomPass;
        CustomPassVolume _customPassVolume;
        RenderTexture _depthTexture;
        Camera _depthCamera;

        public bool TryInitialize() {
            if (TrySetGroundBounds() == false) {
                Log.Important?.Error($"No {nameof(GroundBounds)} in open scenes");
                return false;
            }
            try {
                var parameters = GameConstants.Get.depthTextureStreamingParams;
                var groundBoundsAccess = new GroundBounds.EditorAccess(groundBounds);

                _depthCamera = new GameObject("DepthRenderCamera").AddComponent<Camera>();
                _depthCamera.gameObject.AddComponent<HDAdditionalCameraData>();

                _depthCamera.orthographic = true;
                _depthCamera.cullingMask = cullingMask.value;
                _depthCamera.tag = "MainCamera";
                var groundBoundsHeightDiff = groundBoundsAccess.BoundsTop - groundBoundsAccess.BoundsBottom;
                float unitsPerPixel = 1f / parameters.pixelsPerUnit;
                float smoothingAreaUnitsAdd = (parameters.SmoothingAreaRadiusInPixels * 2 * unitsPerPixel);
                var textureSizeWithSmoothingMarginInUnits = (parameters.chunkTextureSizeInUnits + smoothingAreaUnitsAdd);
                _depthCamera.nearClipPlane = TopDownDepthTexturesLoadingManager.NearClipPlaneDistance;
                _depthCamera.farClipPlane = groundBoundsHeightDiff;
                _depthCamera.orthographicSize = (textureSizeWithSmoothingMarginInUnits * 0.5f);
                _depthCamera.aspect = 1;
                _depthCamera.transform.rotation = TopDownDepthTexturesLoadingManager.CameraRotation;

                var textureSizeWithSmoothingMargin = (parameters.chunkTextureSizeInUnits * parameters.pixelsPerUnit) + (parameters.SmoothingAreaRadiusInPixels * 2);
                var renderTextureDesc = new RenderTextureDescriptor(textureSizeWithSmoothingMargin, textureSizeWithSmoothingMargin, GraphicsFormat.None, GraphicsFormat.D32_SFloat, 0);
                _depthTexture = new(renderTextureDesc);
                _depthTexture.Create();

                _customPassVolume = gameObject.AddComponent<CustomPassVolume>();
                _depthCustomPass = _customPassVolume.AddPassOfType<DepthCustomPass>();
                _depthCustomPass.depthTexture = _depthTexture;
                _customPassVolume.runInEditMode = true;
                _customPassVolume.isGlobal = false;
                _customPassVolume.targetCamera = _depthCamera;
                _customPassVolume.injectionPoint = CustomPassInjectionPoint.BeforeTransparent;
                _depthCustomPass.enabled = false;
            } catch (Exception e) {
                Debug.LogException(e);
                return false;
            }

            return true;
        }

        void OnValidate() {
            TrySetGroundBounds();
        }

        [Button]
        unsafe void CreateTexture(int x, int y) {
            var path = TopDownDepthTexturesLoadingManager.GetTextureFullPath(gameObject.scene.name, new int2(x, y));
            var buffer = FileRead.ToNewBuffer<byte>(path, ARAlloc.Temp);
            var parameters = GameConstants.Get.depthTextureStreamingParams;
            var textureSize = parameters.TextureSize;
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RFloat, 1, false, true);
            texture.LoadRawTextureData((IntPtr)buffer.Ptr, buffer.LengthInt);
            texture.Apply(false, false);
            var textureDirectoryInAssets = Path.Combine(TopDownDepthTexturesLoadingManager.TexturesDirectoryInStreamingAssets, gameObject.scene.name);
            var textureDirectoryFull = Path.Combine(Application.dataPath, textureDirectoryInAssets);
            if (Directory.Exists(textureDirectoryFull) == false) {
                Directory.CreateDirectory(textureDirectoryFull);
            }
            EditorAssetUtil.Create(texture, textureDirectoryInAssets, $"depth_tex_{x}_{y}");
            DestroyImmediate(texture);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Button]
        void BakeLoadedScenes() {
            Bake(false, false).Forget();
        }

        [Button]
        void LoadAndBake() {
            Bake(true, false).Forget();
        }

        [Button]
        void LoadAllAndBake() {
            Bake(true, true).Forget();
        }
        
        public async UniTask Bake(bool loadSubscenes, bool forceLoadDisabledSubscenes) {
            var mapScene = FindAnyObjectByType<MapScene>(FindObjectsInactive.Include);
            if (mapScene == null) {
                Log.Important?.Error($"Cannot bake depth textures. There is no {nameof(MapScene)} in open scenes");
                return;
            }
            if (loadSubscenes) {
                if (mapScene is SubdividedScene) {
                    new SubdividedScene.EditorAccess(mapScene as SubdividedScene).LoadAllScenes(forceLoadDisabledSubscenes);
                }
                await UniTask.DelayFrame(4);
            }

            if (TryInitialize() == false) {
                return;
            }
            
            Camera prevMainCam = SetMainCamToDepthCamera();
            
            var terrainBaker = FindAnyObjectByType<TerrainGroundBoundsBaker>();
            if (terrainBaker) {
                terrainBaker.Bake(groundBounds);
            }

            await UniTask.DelayFrame(2);

            var prevDrakeRendererHighestLodModeState = EditorPrefs.GetBool(DrakeRendererHighestLodModeName);
            EditorPrefs.SetBool(DrakeRendererHighestLodModeName, true);
                
            var drakeStateSystem = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<DrakeRendererStateSystem>();
            if (drakeStateSystem != null) {
                await UniTask.WaitUntil(() => drakeStateSystem.IsLoadingAny == false);
            } else {
                Log.Important?.Error($"No ecs system {nameof(DrakeRendererStateSystem)}");
            }
                

            await UniTask.DelayFrame(4);

            foreach (var lodGroup in FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
                lodGroup.ForceLOD(0);
            }

            GenerateTextures(gameObject.scene, true);
            Dispose();

            if (loadSubscenes) {
                if (mapScene is SubdividedScene) {
                    new SubdividedScene.EditorAccess(mapScene as SubdividedScene).UnloadAllScenes(forceLoadDisabledSubscenes, false);
                }
            }

            AssetDatabase.SaveAssets();

            foreach (var lodGroup in FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
                lodGroup.ForceLOD(-1);
            }

            EditorPrefs.SetBool(DrakeRendererHighestLodModeName, prevDrakeRendererHighestLodModeState);

            if (prevMainCam != null) {
                prevMainCam.enabled = true;
            }
            
            Log.Debug?.Info("Completed wetness textures baking");
        }

        void GenerateTextures(UnityEngine.SceneManagement.Scene mapScene, bool saveAssets) {
            var groundBoundsAccess = new GroundBounds.EditorAccess(groundBounds);
            groundBounds.CalculateGamePolygon(ARAlloc.Temp, out var gamePolygon);
            var gameBounds = gamePolygon.bounds;
            var gameConstants = GameConstants.Get;
            var boundsTop = groundBoundsAccess.BoundsTop;
            var boundsBottom = groundBoundsAccess.BoundsBottom;
            var heightDiff = boundsTop - boundsBottom;
            _depthCustomPass.enabled = true;

            var texturesDir = TopDownDepthTexturesLoadingManager.GetTexturesDirectory(mapScene.name);
            if (Directory.Exists(texturesDir)) {
                Directory.Delete(texturesDir, true);
            } 
            Directory.CreateDirectory(texturesDir);
            var parameters = gameConstants.depthTextureStreamingParams;

            var chunksCount = (int2)math.ceil(gameBounds.Extents / parameters.chunkTextureSizeInUnits);
            var textureSize = parameters.chunkTextureSizeInUnits * parameters.pixelsPerUnit;
            AssetDatabase.StartAssetEditing();

            // depth texture has margin for capturing pixels on edges of chunk texture
            var maskTexture = new RenderTexture(_depthTexture.width, _depthTexture.height, GraphicsFormat.R32_SFloat, GraphicsFormat.None, 1);
            maskTexture.enableRandomWrite = true;
            maskTexture.Create();

            var finalRenderTexture = new RenderTexture(textureSize, textureSize, GraphicsFormat.R32_SFloat, GraphicsFormat.None, 1);
            finalRenderTexture.enableRandomWrite = true;
            finalRenderTexture.Create();

            var finalTexture = new Texture2D(textureSize, textureSize, TextureFormat.RFloat, 1, false, true);

            for (int y = 0; y < chunksCount.y; y++) {
                for (int x = 0; x < chunksCount.x; x++) {
                    try {
                        if (TryGetCameraPositionForChunk(x, y, gameBounds.min, parameters.chunkTextureSizeInUnits, in gamePolygon, out var cameraPos2d) == false) {
                            continue;
                        }
                        _depthCamera.transform.position = new Vector3(cameraPos2d.x, boundsTop, cameraPos2d.y);
                        _depthCamera.Render();
                        var textureCoords = new int2(x, y);
                        var texturePath = TopDownDepthTexturesLoadingManager.GetTextureFullPath(mapScene.name, textureCoords);
                        PostProcessTexture(_depthTexture, maskTexture, finalRenderTexture,
                            postProcessingShader, heightDiff, textureSize, parameters);
                        CopyRenderTextureToTexture(finalRenderTexture, finalTexture);
                        SaveTextureToFile(finalTexture, texturePath);
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }
            AssetDatabase.StopAssetEditing();
            if (saveAssets) {
                AssetDatabase.SaveAssets();
            }

            DestroyImmediate(finalTexture);
            DestroyImmediate(maskTexture);
            DestroyImmediate(finalRenderTexture);
        }

        bool TrySetGroundBounds() {
            if (groundBounds != null) {
                return true;
            }
            var allGroundBounds = FindObjectsByType<GroundBounds>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (allGroundBounds.Length == 1) {
                groundBounds = allGroundBounds[0];
                EditorUtility.SetDirty(this);
            }
            return groundBounds != null;
        }

        static void PostProcessTexture(RenderTexture depthColorTexture, RenderTexture maskTexture, RenderTexture finalTexture,
            ComputeShader postProcessingShader, float heightScaling, int textureSize, DepthTextureStreamingParams parameters) {
            var commands = new CommandBuffer();
            commands.name = "DepthTexturesBaker_PostProcessTexture";

            var constantsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, 1, UnsafeUtility.SizeOf<ShaderConstants>());
            commands.SetBufferData(constantsBuffer, new[] {
                new ShaderConstants(parameters.heightDiffThreshold, parameters.maxHeightDiff, heightScaling, parameters.SmoothingAreaRadiusInPixels, textureSize)
            });
            commands.SetComputeConstantBufferParam(postProcessingShader, ConstantsPropId, constantsBuffer, 0, UnsafeUtility.SizeOf<ShaderConstants>());
            {
                int initializeMaskTextureKernelIndex = postProcessingShader.FindKernel(InitializeMaskKernelName);
                postProcessingShader.GetKernelThreadGroupSizes(initializeMaskTextureKernelIndex, out var groupsCountX, out var groupsCountY, out _);
                var textureSizeWithOffsets = maskTexture.height;
                commands.SetComputeTextureParam(postProcessingShader, initializeMaskTextureKernelIndex, MaskPropId, maskTexture);
                commands.DispatchCompute(postProcessingShader, initializeMaskTextureKernelIndex,
                    Mathf.CeilToInt((float)textureSizeWithOffsets / groupsCountX), Mathf.CeilToInt((float)textureSizeWithOffsets / groupsCountY), 1);
            }
            {
                int setMaskTextureKernelIndex = postProcessingShader.FindKernel(SetMaskKernelName);
                postProcessingShader.GetKernelThreadGroupSizes(setMaskTextureKernelIndex, out var groupsCountX, out var groupsCountY, out _);

                commands.SetComputeTextureParam(postProcessingShader, setMaskTextureKernelIndex, DepthMapPropId, depthColorTexture);
                commands.SetComputeTextureParam(postProcessingShader, setMaskTextureKernelIndex, MaskPropId, maskTexture);
                commands.DispatchCompute(postProcessingShader, setMaskTextureKernelIndex,
                    Mathf.CeilToInt((float)textureSize / groupsCountX), Mathf.CeilToInt((float)textureSize / groupsCountY), 1);
            }

            {
                int copyToFinalTextureKernelIndex = postProcessingShader.FindKernel(SmoothAndWriteToFinalTextureKernelName);
                postProcessingShader.GetKernelThreadGroupSizes(copyToFinalTextureKernelIndex, out var groupsCountX, out var groupsCountY, out _);
                commands.SetComputeTextureParam(postProcessingShader, copyToFinalTextureKernelIndex, MaskPropId, maskTexture);
                commands.SetComputeTextureParam(postProcessingShader, copyToFinalTextureKernelIndex, DepthMapPropId, depthColorTexture);
                commands.SetComputeTextureParam(postProcessingShader, copyToFinalTextureKernelIndex, FinalTexturePropId, finalTexture);
                commands.DispatchCompute(postProcessingShader, copyToFinalTextureKernelIndex,
                    Mathf.CeilToInt((float)textureSize / groupsCountX), Mathf.CeilToInt((float)textureSize / groupsCountY), 1);
            }
            UnityEngine.Graphics.ExecuteCommandBuffer(commands);
            commands.Dispose();
            constantsBuffer.Dispose();
        }

        static void CopyRenderTextureToTexture(RenderTexture renderTexture, Texture2D texture) {
            RenderTexture prevActiveRenderTexture = RenderTexture.active;

            RenderTexture.active = renderTexture;
            var width = renderTexture.width;
            var height = renderTexture.height;

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply(false, false);
            RenderTexture.active = prevActiveRenderTexture;
        }

        static void SaveTextureToFile(Texture2D texture, string path) {
            var textureBytes = texture.GetRawTextureData<byte>();
            var fileStream = new FileStream(path, FileMode.OpenOrCreate);
            fileStream.Write(textureBytes);
            fileStream.Dispose();
        }

        static bool TryGetCameraPositionForChunk(int x, int y, float2 gameBoundsMin, int chunkTextureSizeInUnits, in Polygon2D gamePolygon, out float2 cameraPos) {
            GetChunkMinMax(x, y, gameBoundsMin, chunkTextureSizeInUnits, out var minCornerPos, out var maxCornerPos);
            Polygon2DUtils.IsInPolygon(minCornerPos, in gamePolygon, out var isCorner0InPolygon);
            Polygon2DUtils.IsInPolygon(maxCornerPos, in gamePolygon, out var isCorner1InPolygon);
            Polygon2DUtils.IsInPolygon(new float2(minCornerPos.x, maxCornerPos.y), in gamePolygon, out var isCorner2InPolygon);
            Polygon2DUtils.IsInPolygon(new float2(minCornerPos.y, maxCornerPos.x), in gamePolygon, out var isCorner3InPolygon);
            bool isAnyCornerInPolygon = isCorner0InPolygon | isCorner1InPolygon | isCorner2InPolygon | isCorner3InPolygon;
            if (!isAnyCornerInPolygon) {
                cameraPos = default;
                return false;
            }
            cameraPos = math.lerp(minCornerPos, maxCornerPos, 0.5f);
            return true;
        }

        static void GetChunkMinMax(int x, int y, float2 gameBoundsMin, int chunkTextureSizeInUnits, out float2 min, out float2 max) {
            min = gameBoundsMin + new float2(chunkTextureSizeInUnits * x, chunkTextureSizeInUnits * y);
            max = min + new float2(chunkTextureSizeInUnits);
        }

        Camera SetMainCamToDepthCamera() {
            _depthCamera.enabled = false;
            var prevMainCam = Camera.main;
            if (prevMainCam != null) {
                prevMainCam.enabled = false;
            }
            _depthCamera.enabled = true;
            return prevMainCam;
        }
        
        void Dispose() {
            if (_depthCamera != null) {
                DestroyImmediate(_depthCamera.gameObject);
                _depthCamera = null;
            }
            if (_depthCustomPass != null) {
                _depthCustomPass.depthTexture = null;
                _depthCustomPass = null;
            }
            if (_customPassVolume != null) {
                DestroyImmediate(_customPassVolume);
                _customPassVolume = null;
            }
            if (_depthTexture != null) {
                _depthTexture.Release();
                DestroyImmediate(_depthTexture);
                _depthTexture = null;
            }
        }

        void OnDrawGizmos() {
            if (groundBounds != null && previewChunks) {
                groundBounds.CalculateGamePolygon(ARAlloc.Temp, out var gamePolygon);
                var gameBounds = gamePolygon.bounds;
                var parameters = GameConstants.Get.depthTextureStreamingParams;
                var chunkTextureSizeInUnits = parameters.chunkTextureSizeInUnits;
                var chunksCount = (int2)math.ceil(gameBounds.Extents / chunkTextureSizeInUnits);
                var groundBoundsAccess = new GroundBounds.EditorAccess(groundBounds);
                var boundsTop = groundBoundsAccess.BoundsTop;
                var boundsBottom = groundBoundsAccess.BoundsBottom;
                var boundsHeight = boundsTop - boundsBottom;
                var boundsCenterY = math.lerp(boundsBottom, boundsTop, 0.5f);
                var styleBold = new GUIStyle {
                    fontStyle = FontStyle.Bold,
                    fontSize = fontSize,
                    normal = { textColor = Color.white },
                    richText = true
                };
                var cameraFrustumCubeSize = new Vector3(chunkTextureSizeInUnits, boundsHeight, chunkTextureSizeInUnits);
                for (int y = 0; y < chunksCount.y; y++) {
                    for (int x = 0; x < chunksCount.x; x++) {
                        if (TryGetCameraPositionForChunk(x, y, gameBounds.min, chunkTextureSizeInUnits, in gamePolygon, out var cameraPos2d) == false) {
                            continue;
                        }
                        var cameraFrustumCubeCenter = new Vector3(cameraPos2d.x, boundsCenterY, cameraPos2d.y);
                        Gizmos.DrawWireCube(cameraFrustumCubeCenter, cameraFrustumCubeSize);
                        UnityEditor.Handles.Label(cameraFrustumCubeCenter, $"({x}, {y})", styleBold);
                    }
                }
            }
        }

        struct ShaderConstants {
            public float HeightDiffThreshold; // The minimum absolute difference required to mark the pixel as discontinuous.
            public float HeightDiffMax;
            public float HeightScaling;
            public int SmoothingAreaRadiusInPixels; // Provided texture is bigger than area which needs to be sampled, these params specify actual area of texture
            public int TextureSize;

            public ShaderConstants(float heightDiffThreshold, float heightDiffMax, float heightScaling, int smoothingAreaRadiusInPixels, int textureSize) {
                HeightDiffThreshold = heightDiffThreshold;
                HeightDiffMax = heightDiffMax;
                HeightScaling = heightScaling;
                SmoothingAreaRadiusInPixels = smoothingAreaRadiusInPixels;
                TextureSize = textureSize;
            }
        }
#endif
    }
}
using System;
using System.IO;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Cameras;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Awaken.Utility.Files;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UniversalProfiling;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Graphics.VFX {
    public class TopDownDepthTexturesLoadingManager : MonoBehaviour, UnityUpdateProvider.IWithUpdateGeneric, UnityUpdateProvider.IWithLateUpdateGeneric {
        public const string TexturesDirectoryInStreamingAssets = "DepthTextures";
        public const float NearClipPlaneDistance = 0.01f;
        public static readonly Quaternion CameraRotation = Quaternion.Euler(90, 0, 0);

        const int PreAllocTexturesCount = 6;
        const int TexturesArrayLayersCount = 4;
        const float NullOffset = -99999999f;
        const float MaxLoadingAreaScale = 0.95f;
        const string CopyDataBufferToTexturesArrayKernelName = "CopyDataBufferToTexturesArray";
        static readonly int TexturesArrayPropId = Shader.PropertyToID("TexturesArray");
        static readonly int DataBufferPropId = Shader.PropertyToID("DataBuffer");
        static readonly int TextureSizePropId = Shader.PropertyToID("TextureSize");
        static readonly int ToLayerPropId = Shader.PropertyToID("ToLayer");
        static readonly UniversalProfilerMarker CopyTextureDataPartMarker = new("TopDownDepthTexturesLoadingManager.CopyTextureDataPartToGpuBuffer");
        static readonly UniversalProfilerMarker DispatchComputeShaderMarker = new("TopDownDepthTexturesLoadingManager.DispatchComputeShader");

        static TexturesDirectoryData s_currentTexturesDirectory;

        [SerializeField, Required] GroundBounds groundBounds;
        [SerializeField, Required] ComputeShader wetnessTexturesArrayDataSetShader;

        [SerializeField, Range(0.1f, 0.9f)] float visibleAreaRectScale = 0.65f;
        [SerializeField, Range(0.1f, 0.9f)] float preloadChunksScaleAdd = 0.25f;
#if UNITY_EDITOR
        [ShowInInspector] bool _previewBounds;
        [ShowInInspector] bool _previewTextures;
        [ShowInInspector, ShowIf(nameof(_previewTextures)), Range(0.1f, 1f)] float previewTexturesAlpha = 0.5f;
        EditorPreviewData _previewData = new(PreAllocTexturesCount);
#endif
        // --- State params
        StructList<ChunkIndexWithLoadingState> _loadingChunksStates = new(PreAllocTexturesCount);
        StructList<TextureLoadFromDiskData> _loadingFromDiskDatas = new(PreAllocTexturesCount);
        StructList<TextureLoadToGpuData> _loadingToGpuDatas = new(PreAllocTexturesCount);
        StructList<TextureLoadToGpuData> _cancelledWaitingForCleanupLoadingToGpuDatas = new(PreAllocTexturesCount);
        StretchingRect _loadingAreaRect;
        MinMaxAABR _visibleAreaRect;
        int4 _loadedToGpuChunks = -1;
        int4 _reservedToLoadToGpuChunks = -1;

        // --- Constant params
        int _chunkTextureSizeInUnits;
        int _textureDataMaxBytesToCopyPerFrame;
        int _chunkTextureSizeInBytes;
        float _textureSizeInUnitsRcp;
        int2 _chunksMaxCountXY;
        MinMaxAABR _gameBounds2d;
        UnsafeBitmask _chunksValidStatuses;
        string _mainSceneName;
        public bool IsInitialized => DepthTexturesArray != null;
        public int ChunkTextureSize { get; private set; }
        public float DepthTextureRcpUVScale { get; private set; }
        public float NearPlane { get; private set; }
        public float FarPlane { get; private set; }
        public Matrix4x4 CameraViewToClipMatrix { get; private set; }
        public float CameraWorldPosY { get; private set; }
        public Matrix4x4 WorldToCameraMatrix { get; private set; }
        public Matrix4x4 WorldToCameraMatrixFlippedZ { get; private set; }
        public Matrix4x4 CameraProjectionMatrix { get; private set; }
        public Matrix4x4 CameraViewProjectionMatrix => CameraProjectionMatrix * WorldToCameraMatrixFlippedZ;
        public RenderTexture DepthTexturesArray { get; private set; }
        public float2 TexBottomLeftUVOffset { get; private set; }
        public float2 TexBottomRightUVOffset { get; private set; }
        public float2 TexTopLeftUVOffset { get; private set; }
        public float2 TexTopRightUVOffset { get; private set; }
        public float4 DepthTexturesLayers { get; private set; }
        public float MaxHeightDiff => GameConstants.Get.depthTextureStreamingParams.maxHeightDiff;

        void Awake() {
            enabled = false;
        }

        void Start() {
            ResetState();
            InitializeConstantData();
            _mainSceneName = gameObject.scene.name;
            var chunksMaxCount = (uint)(_chunksMaxCountXY.x * _chunksMaxCountXY.y);
            for (uint chunkIndex = 0; chunkIndex < chunksMaxCount; chunkIndex++) {
                var texturePath = GetTextureFullPath(_mainSceneName, GetChunkCoord((int)chunkIndex));
                _chunksValidStatuses[chunkIndex] = File.Exists(texturePath);
            }
            if (AreTexturesFilesValid() == false) {
                Log.Critical?.Error($"Wetness Depth textures size does not match texture size in game constants. You need to bake wetness depth textures using {nameof(TopDownDepthTextureBaker)}");
                Destroy(this);
            }

            bool AreTexturesFilesValid() {
                var firstValidChunkIndex = _chunksValidStatuses.FirstOne();
                if (firstValidChunkIndex == -1) {
                    return false;
                }
                var texturePath = GetTextureFullPath(_mainSceneName, GetChunkCoord(firstValidChunkIndex));
                var fileSize = FileRead.GetFileInfo(texturePath).FileSize;
                if (fileSize != _chunkTextureSizeInBytes) {
                    return false;
                }
                return true;
            }
        }

        void OnEnable() {
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
            UnityUpdateProvider.GetOrCreate().UnregisterLateGeneric(this);
        }

        void OnDisable() {
            CancelLoadingAll(false);
            ResetState();
            UnityUpdateProvider.GetOrCreate().UnregisterGeneric(this);
        }

        void OnDestroy() {
            UnityUpdateProvider.GetOrCreate().UnregisterGeneric(this);
            UnityUpdateProvider.GetOrCreate().UnregisterLateGeneric(this);
            CancelLoadingAll(true);
            ResetState();

            _loadingChunksStates.Clear();
            for (int i = 0; i < _loadingFromDiskDatas.Count; i++) {
                ref var dataRef = ref _loadingFromDiskDatas[i];
                dataRef.DisposeAll();
            }
            _loadingFromDiskDatas.Clear();
            for (int i = 0; i < _loadingToGpuDatas.Count; i++) {
                ref var dataRef = ref _loadingToGpuDatas[i];
                dataRef.DisposeAll();
            }
            _loadingToGpuDatas.Clear();
            if (_chunksValidStatuses.IsCreated) {
                _chunksValidStatuses.Dispose();
            }
            if (DepthTexturesArray?.IsCreated() == true) {
                DepthTexturesArray.Release();
            }
#if UNITY_EDITOR
            for (int i = 0; i < _previewData.loadedTexturesPreviewData.Count; i++) {
                var previewMaterialInstance = _previewData.loadedTexturesPreviewData[i].previewMaterialInstance;
                Destroy(previewMaterialInstance.mainTexture);
                Destroy(previewMaterialInstance);
            }
            _previewData.loadedTexturesPreviewData.Clear();

            if (_previewData.previewMaterial != null) {
                Destroy(_previewData.previewMaterial);
                _previewData.previewMaterial = null;
            }
#endif
        }

        public unsafe void UnityUpdate() {
            if (Hero.Current == null || DepthTexturesArray == null) {
                return;
            }

            var heroPos2d = ((float3)Hero.Current.Coords).xz;
            float2 heroPosInGameBoundsSpace = heroPos2d - _gameBounds2d.min;

            WorldToCameraMatrix = Matrix4x4.TRS(new Vector3(heroPos2d.x, CameraWorldPosY, heroPos2d.y), CameraRotation, new Vector3(1, 1, 1)).inverse;
            Matrix4x4 flipZMatrix = Matrix4x4.Scale(new Vector3(1, 1, -1));
            WorldToCameraMatrixFlippedZ = flipZMatrix * WorldToCameraMatrix;

            GetChunksToLoadMinMax(_loadingAreaRect, out var prevChunksToLoadAreaMinCoord, out var prevChunksToLoadAreaMaxCoord);
            UpdateLoadingAreaRect(ref _loadingAreaRect, heroPosInGameBoundsSpace);
            GetChunksToLoadMinMax(_loadingAreaRect, out var chunksToLoadAreaMinCoord, out var chunksToLoadAreaMaxCoord);

            if ((prevChunksToLoadAreaMinCoord.Equals(chunksToLoadAreaMinCoord) == false) | (prevChunksToLoadAreaMaxCoord.Equals(chunksToLoadAreaMaxCoord) == false)) {
                var chunksToLoadCountX = chunksToLoadAreaMaxCoord.x - chunksToLoadAreaMinCoord.x + 1;
                var chunksToLoadCountY = chunksToLoadAreaMaxCoord.y - chunksToLoadAreaMinCoord.y + 1;
                Span<int> currentNeededChunks = stackalloc int[chunksToLoadCountX * chunksToLoadCountY];
                int currentNeededChunksCount = 0;
                for (int y = chunksToLoadAreaMinCoord.y; y <= chunksToLoadAreaMaxCoord.y; y++) {
                    for (int x = chunksToLoadAreaMinCoord.x; x <= chunksToLoadAreaMaxCoord.x; x++) {
                        var chunkIndex = GetChunkIndex(x, y);
                        if (chunkIndex != -1 && _chunksValidStatuses[(uint)chunkIndex]) {
                            currentNeededChunks[currentNeededChunksCount++] = chunkIndex;
                        }
                    }
                }
                for (int i = currentNeededChunksCount; i < currentNeededChunks.Length; i++) {
                    currentNeededChunks[i] = -1;
                }

                // cancel loading not needed chunks
                for (int i = _loadingChunksStates.Count - 1; i >= 0; i--) {
                    var chunkState = _loadingChunksStates[i];
                    if (chunkState.chunkIndex != -1 && currentNeededChunks.Contains(chunkState.chunkIndex) == false) {
                        CancelLoadingChunkTexture(chunkState, false);
                        _loadingChunksStates.RemoveAtSwapBack(i);
                    }
                }

                // unreserve texturesArray layers for not needed chunks
                for (int i = 0; i < 4; i++) {
                    var chunkIndex = _reservedToLoadToGpuChunks[i];
                    if (chunkIndex != -1) {
                        var chunkCoord = GetChunkCoord(chunkIndex);
                        if (chunkCoord.x < chunksToLoadAreaMinCoord.x || chunkCoord.y < chunksToLoadAreaMinCoord.y ||
                            chunkCoord.x > chunksToLoadAreaMaxCoord.x || chunkCoord.y > chunksToLoadAreaMaxCoord.y) {
                            _reservedToLoadToGpuChunks[i] = -1;
                        }
                    }
                }

                // add chunk states for needed chunks so later they would be loaded
                for (int i = 0; i < currentNeededChunksCount; i++) {
                    var neededToLoadChunkIndex = currentNeededChunks[i];
                    if (mathExt.IndexOf(neededToLoadChunkIndex, _reservedToLoadToGpuChunks) == -1 && mathExt.IndexOf(neededToLoadChunkIndex, _loadedToGpuChunks) == -1) {
                        _loadingChunksStates.Add(new ChunkIndexWithLoadingState(neededToLoadChunkIndex, ChunkLoadingState.None));
                    }
                }
            }

            UpdateChunksLoading();

            var visibleAreaRectSizeInUnits = _chunkTextureSizeInUnits * visibleAreaRectScale;
            var visibleAreaRectRcpSizeInUnits = math.rcp(visibleAreaRectSizeInUnits);
            var visibleRectHalfSize = visibleAreaRectSizeInUnits * 0.5f;
            _visibleAreaRect = new MinMaxAABR(
                (heroPosInGameBoundsSpace - new float2(visibleRectHalfSize)),
                (heroPosInGameBoundsSpace + new float2(visibleRectHalfSize)));

            var visibleAreaRectMinCoord = (int2)math.floor(_visibleAreaRect.min * _textureSizeInUnitsRcp);
            var visibleAreaRectMaxCoord = (int2)math.floor(_visibleAreaRect.max * _textureSizeInUnitsRcp);

            GetLoadedTexturesLayers(visibleAreaRectMinCoord, visibleAreaRectMaxCoord, out int4 loadedTexturesLayers);

            var bottomLeftTextureMinCornerPosInGameBoundsSpace = ((float2)(visibleAreaRectMinCoord) * _chunkTextureSizeInUnits);
            var bottomLeftTextureOffset = bottomLeftTextureMinCornerPosInGameBoundsSpace - _visibleAreaRect.min;
            var bottomRightTextureOffset = (bottomLeftTextureMinCornerPosInGameBoundsSpace + new float2(_chunkTextureSizeInUnits, 0)) - _visibleAreaRect.min;
            var topLeftTextureOffset = (bottomLeftTextureMinCornerPosInGameBoundsSpace + new float2(0, _chunkTextureSizeInUnits)) - _visibleAreaRect.min;
            var topRightTextureOffset = (bottomLeftTextureMinCornerPosInGameBoundsSpace + new float2(_chunkTextureSizeInUnits, _chunkTextureSizeInUnits)) - _visibleAreaRect.min;

            TexBottomLeftUVOffset = loadedTexturesLayers.x != -1 ? (bottomLeftTextureOffset * visibleAreaRectRcpSizeInUnits) : NullOffset;
            TexBottomRightUVOffset = loadedTexturesLayers.y != -1 ? (bottomRightTextureOffset * visibleAreaRectRcpSizeInUnits) : NullOffset;
            TexTopLeftUVOffset = loadedTexturesLayers.z != -1 ? (topLeftTextureOffset * visibleAreaRectRcpSizeInUnits) : NullOffset;
            TexTopRightUVOffset = loadedTexturesLayers.w != -1 ? (topRightTextureOffset * visibleAreaRectRcpSizeInUnits) : NullOffset;

            DepthTexturesLayers = loadedTexturesLayers;

            CheckIfCancelledComputeShadersFinishedAndDispose();
#if UNITY_EDITOR
            if (_previewTextures) {
                var chunksToLoadCountX = chunksToLoadAreaMaxCoord.x - chunksToLoadAreaMinCoord.x + 1;
                var chunksToLoadCountY = chunksToLoadAreaMaxCoord.y - chunksToLoadAreaMinCoord.y + 1;
                Span<int> currentNeededChunks = stackalloc int[chunksToLoadCountX * chunksToLoadCountY];
                int chunkToLoadIterator = 0;
                for (int y = chunksToLoadAreaMinCoord.y; y <= chunksToLoadAreaMaxCoord.y; y++) {
                    for (int x = chunksToLoadAreaMinCoord.x; x <= chunksToLoadAreaMaxCoord.x; x++) {
                        var chunkIndex = GetChunkIndex(x, y);
                        if (chunkIndex != -1 && _chunksValidStatuses[(uint)chunkIndex]) {
                            currentNeededChunks[chunkToLoadIterator++] = GetChunkIndex(x, y);
                        }
                    }
                }
                for (int i = 0; i < _previewData.loadedTexturesPreviewData.Count; i++) {
                    var previewData = _previewData.loadedTexturesPreviewData[i];
                    if (currentNeededChunks.Contains(previewData.chunkIndex)) {
                        UnityEngine.Graphics.DrawMesh(_previewData.quadMesh, previewData.matrix, previewData.previewMaterialInstance, 1);
                    }
                }
            }
#endif
        }

        // Cleanup update
        public void UnityLateUpdate(float _) {
            bool disposedAll = CheckIfCancelledComputeShadersFinishedAndDispose();
            if (disposedAll) {
                UnityUpdateProvider.GetOrCreate().UnregisterLateGeneric(this);
            }
        }

        public void SetDepthTexturesLoadingEnabled(bool enable) {
            this.enabled = enable;
        }

        void InitializeConstantData() {
            if (groundBounds == null) {
                groundBounds = GameObjects.FindComponentByTypeInScene<GroundBounds>(gameObject.scene, false);
            }
            if (groundBounds == null) {
                Log.Important?.Error($"No {nameof(GroundBounds)} in scene {gameObject.scene}. Destroying {nameof(TopDownDepthTexturesLoadingManager)}");
                Destroy(this);
                return;
            }
            var gameBounds3d = groundBounds.CalculateGameBounds();
            SetConstantParams(gameBounds3d);

            DepthTexturesArray = new RenderTexture(ChunkTextureSize, ChunkTextureSize, 0, GraphicsFormat.R32_SFloat);
            DepthTexturesArray.dimension = TextureDimension.Tex2DArray;
            DepthTexturesArray.enableRandomWrite = true;
            DepthTexturesArray.filterMode = FilterMode.Point;
            DepthTexturesArray.wrapMode = TextureWrapMode.Repeat;
            DepthTexturesArray.volumeDepth = TexturesArrayLayersCount;
            DepthTexturesArray.Create();
#if UNITY_EDITOR
            _previewData.gameBoundsMaxY = gameBounds3d.max.y;
            _previewData.quadMesh = EDITOR_CreateQuadMesh();
            _previewData.previewMaterial = new Material(GameConstants.Get.EDITOR_hdrpUnlitMaterial);
#endif
        }

        void SetConstantParams(Bounds gameBounds3d) {
            _gameBounds2d = new MinMaxAABR(((float3)gameBounds3d.min).xz, ((float3)gameBounds3d.max).xz);
            var gameBoundsMinMaxY = new float2(gameBounds3d.min.y, gameBounds3d.max.y);

            var parameters = GameConstants.Get.depthTextureStreamingParams;
            _chunkTextureSizeInUnits = parameters.chunkTextureSizeInUnits;
            _textureSizeInUnitsRcp = 1f / _chunkTextureSizeInUnits;
            ChunkTextureSize = parameters.chunkTextureSizeInUnits * parameters.pixelsPerUnit;
            DepthTextureRcpUVScale = visibleAreaRectScale;
            _textureDataMaxBytesToCopyPerFrame = parameters.textureDataMaxBytesToCopyPerFrame;

            NearPlane = NearClipPlaneDistance;
            FarPlane = gameBoundsMinMaxY.y - gameBoundsMinMaxY.x;
            float orthographicSize = _chunkTextureSizeInUnits * visibleAreaRectScale * 0.5f;

            CameraViewToClipMatrix = CameraUtils.GetOrthographicViewToClipMatrix(orthographicSize, NearClipPlaneDistance, FarPlane);

            CameraProjectionMatrix = Matrix4x4.Ortho(-orthographicSize, orthographicSize, -orthographicSize, orthographicSize, NearClipPlaneDistance, FarPlane);

            CameraWorldPosY = gameBoundsMinMaxY.y;

            _chunkTextureSizeInBytes = (int)DepthTextureStreamingParams.GetTextureSizeInBytes(ChunkTextureSize);
            _chunksMaxCountXY = (int2)math.ceil(_gameBounds2d.Extents * _textureSizeInUnitsRcp);

            var chunksMaxCount = (uint)(_chunksMaxCountXY.x * _chunksMaxCountXY.y);
            _chunksValidStatuses = new UnsafeBitmask(chunksMaxCount, ARAlloc.Persistent);
        }

        void ResetState() {
            _loadingAreaRect = default;
            _visibleAreaRect = default;
            _loadedToGpuChunks = -1;
            _reservedToLoadToGpuChunks = -1;
        }

        void GetLoadedTexturesLayers(int2 rectMinCoord, int2 rectMaxCoord, out int4 texturesLayers) {
            var rectChunksCountXY = rectMaxCoord - rectMinCoord + new int2(1);
            var rectChunksCount = rectChunksCountXY.x * rectChunksCountXY.y;
            texturesLayers = new int4(-1);
            if (rectChunksCount == 1) {
                var chunkIndex = GetChunkIndex(rectMinCoord);
                if (TryGetLayerWhereTextureIsLoadedInTexturesArray(chunkIndex, out var bottomLeftTextureLayer)) {
                    texturesLayers = new int4(bottomLeftTextureLayer, -1, -1, -1);
                }
            } else if (rectChunksCount == 2) {
                var texture0Index = GetChunkIndex(rectMinCoord);
                var texture1Index = GetChunkIndex(rectMaxCoord);
                bool isTexture0InTexturesArray = TryGetLayerWhereTextureIsLoadedInTexturesArray(texture0Index, out var texture0TextureArrayLayer);
                bool isTexture1InTexturesArray = TryGetLayerWhereTextureIsLoadedInTexturesArray(texture1Index, out var texture1TextureArrayLayer);
                // bottom left + top left
                if (rectMinCoord.x == rectMaxCoord.x) {
                    if (isTexture0InTexturesArray || isTexture1InTexturesArray) {
                        if (isTexture0InTexturesArray && isTexture1InTexturesArray) {
                            texturesLayers = new int4(texture0TextureArrayLayer, -1, texture1TextureArrayLayer, -1);
                        } else if (isTexture0InTexturesArray) {
                            texturesLayers = new int4(texture0TextureArrayLayer, -1, -1, -1);
                        } else {
                            texturesLayers = new int4(-1, -1, texture1TextureArrayLayer, -1);
                        }
                    }
                }
                // bottom left + bottom right
                else {
                    if (isTexture0InTexturesArray || isTexture1InTexturesArray) {
                        if (isTexture0InTexturesArray && isTexture1InTexturesArray) {
                            texturesLayers = new int4(texture0TextureArrayLayer, texture1TextureArrayLayer, -1, -1);
                        } else if (isTexture0InTexturesArray) {
                            texturesLayers = new int4(texture0TextureArrayLayer, -1, -1, -1);
                        } else {
                            texturesLayers = new int4(-1, texture1TextureArrayLayer, -1, -1);
                        }
                    }
                }
            } else {
                var bottomLeftChunkIndex = GetChunkIndex(rectMinCoord);
                var bottomRightChunkIndex = GetChunkIndex(rectMaxCoord.x, rectMinCoord.y);
                var topLeftChunkIndex = GetChunkIndex(rectMinCoord.x, rectMaxCoord.y);
                var topRightChunkIndex = GetChunkIndex(rectMaxCoord);

                if (TryGetLayerWhereTextureIsLoadedInTexturesArray(bottomLeftChunkIndex, out var layerWhereBottomLeftIsSet) &
                    TryGetLayerWhereTextureIsLoadedInTexturesArray(bottomRightChunkIndex, out var layerWhereBottomRightIsSet) &
                    TryGetLayerWhereTextureIsLoadedInTexturesArray(topLeftChunkIndex, out var layerWhereTopLeftIsSet) &
                    TryGetLayerWhereTextureIsLoadedInTexturesArray(topRightChunkIndex, out var layerWhereTopRightIsSet)) {
                    texturesLayers = new int4(layerWhereBottomLeftIsSet, layerWhereBottomRightIsSet, layerWhereTopLeftIsSet, layerWhereTopRightIsSet);
                } else {
                    if (layerWhereBottomLeftIsSet != -1) {
                        texturesLayers.x = layerWhereBottomLeftIsSet;
                    }
                    if (layerWhereBottomRightIsSet != -1) {
                        texturesLayers.y = layerWhereBottomRightIsSet;
                    }
                    if (layerWhereTopLeftIsSet != -1) {
                        texturesLayers.z = layerWhereTopLeftIsSet;
                    }
                    if (layerWhereTopRightIsSet != -1) {
                        texturesLayers.w = layerWhereTopRightIsSet;
                    }
                }
            }
        }

        void UpdateChunksLoading() {
            Array.Sort(_loadingChunksStates.BackingArray, 0, _loadingChunksStates.Count);
            bool executedExpensiveOperation = false;
            for (int i = _loadingChunksStates.Count - 1; i >= 0; i--) {
                ref var chunkStateRef = ref _loadingChunksStates[i];
                switch (chunkStateRef.state) {
                    case ChunkLoadingState.None: {
                        StartLoadingChunkFromDisk(chunkStateRef, out var cancel, out var startedLoading, ref executedExpensiveOperation);
                        if (cancel) {
                            _loadingChunksStates.RemoveAtSwapBack(i);
                        } else if (startedLoading) {
                            chunkStateRef.state = ChunkLoadingState.LoadingFromDisk;
                        }
                        break;
                    }
                    case ChunkLoadingState.LoadingFromDisk: {
                        ProcessLoadingChunkFromDisk(chunkStateRef, out var cancel, out var startedLoadingToGpu, out var layerWhereLoading, ref executedExpensiveOperation);
                        if (cancel) {
                            _loadingChunksStates.RemoveAtSwapBack(i);
                        } else if (startedLoadingToGpu) {
                            _reservedToLoadToGpuChunks[layerWhereLoading] = chunkStateRef.chunkIndex;
                            chunkStateRef.state = ChunkLoadingState.LoadingToGpuBuffer;
                        }
                        break;
                    }
                    case ChunkLoadingState.LoadingToGpuBuffer: {
                        ProcessLoadingChunkToGpu(chunkStateRef, out var cancel, out var loaded, out var layerWhereLoaded, ref executedExpensiveOperation);
                        if (cancel) {
                            _loadingChunksStates.RemoveAtSwapBack(i);
                        } else if (loaded) {
                            _loadedToGpuChunks[layerWhereLoaded] = chunkStateRef.chunkIndex;
                            if (_reservedToLoadToGpuChunks[layerWhereLoaded] != chunkStateRef.chunkIndex) {
                                Log.Important?.Error($"Chunk {chunkStateRef.chunkIndex} loaded on layer {layerWhereLoaded} but it is not reserved on that layer. Reserved layers {_reservedToLoadToGpuChunks}");
                            }
                            _loadingChunksStates.RemoveAtSwapBack(i);
                        }
                        break;
                    }
                }
            }
        }

        void StartLoadingChunkFromDisk(ChunkIndexWithLoadingState chunkState, out bool cancel, out bool startedLoading, ref bool executedExpensiveOperation) {
            startedLoading = false;
            cancel = false;
            if (executedExpensiveOperation) {
                return;
            }
            TextureLoadFromDiskData data;
            try {
                var chunkIndex = chunkState.chunkIndex;
                if (_loadedToGpuChunks.Contains(chunkIndex)) {
                    cancel = true;
                    return;
                }
                var chunkCoord = GetChunkCoord(chunkIndex);
                if (math.any(chunkCoord == -1)) {
                    Log.Important?.Error($"Converting {chunkState.chunkIndex} to chunk coord resulted in coord {chunkCoord}");
                    cancel = true;
                    return;
                }
                var texturePath = GetTextureFullPath(_mainSceneName, GetChunkCoord(chunkIndex));
                var readHandle = FileRead.ToNewBufferAsync(texturePath, 0, (uint)_chunkTextureSizeInBytes, Allocator.Persistent,
                    out UnsafeArray<byte> textureData,
                    out UnsafeArray<ReadCommand> commands);
                data = new TextureLoadFromDiskData(chunkIndex, textureData, readHandle, commands);
            } catch (Exception e) {
                Log.Important?.Error($"Failed to load chunk {chunkState.chunkIndex}. Exception below");
                Debug.LogException(e);
                cancel = true;
                return;
            }
            _loadingFromDiskDatas.Add(data);
            startedLoading = true;
            executedExpensiveOperation = true;
        }

        void ProcessLoadingChunkFromDisk(ChunkIndexWithLoadingState chunkState, out bool cancel, out bool startedLoadingToGpu, out short layerWhereLoading, ref bool executedExpensiveOperation) {
            cancel = false;
            startedLoadingToGpu = false;
            layerWhereLoading = -1;
            if (executedExpensiveOperation) {
                return;
            }
            try {
                if (_loadedToGpuChunks.Contains(chunkState.chunkIndex)) {
                    cancel = true;
                    return;
                }
                if (TryGetIndexOfLoadingFromDiskData(chunkState.chunkIndex, _loadingFromDiskDatas, out var indexInArr)) {
                    ref TextureLoadFromDiskData dataRef = ref _loadingFromDiskDatas[indexInArr];
                    if (dataRef.readHandle.Status != ReadStatus.InProgress) {
                        dataRef.DisposeNotSharedData();
                        var textureData = dataRef.textureData;
                        _loadingFromDiskDatas.RemoveAtSwapBack(indexInArr);
                        startedLoadingToGpu = true;

                        StartLoadingToGpu(chunkState.chunkIndex, textureData, out cancel, out layerWhereLoading);
                        if (cancel) {
                            return;
                        }
                        executedExpensiveOperation = true;
#if UNITY_EDITOR
                        if (_previewData.previewMaterial != null) {
                            var previewMaterialInstance = new Material(_previewData.previewMaterial);
                            previewMaterialInstance.color = new Color(1, 1, 1, previewTexturesAlpha);
                            var previewTexture = new Texture2D(ChunkTextureSize, ChunkTextureSize, TextureFormat.RFloat, 1, false, true);
                            unsafe {
                                previewTexture.LoadRawTextureData((IntPtr)textureData.Ptr, textureData.LengthInt);
                            }
                            previewTexture.Apply(false, true);
                            previewMaterialInstance.mainTexture = previewTexture;
                            var textureCoord = GetChunkCoord(chunkState.chunkIndex);
                            var center2d = _gameBounds2d.min + ((float2)textureCoord * _chunkTextureSizeInUnits) + new float2(_chunkTextureSizeInUnits * 0.5f);
                            var textureCenterMatrix = Matrix4x4.TRS(new Vector3(center2d.x, _previewData.gameBoundsMaxY, center2d.y), Quaternion.identity, new float3(_chunkTextureSizeInUnits));
                            _previewData.loadedTexturesPreviewData.Add(new TexturePreviewData(chunkState.chunkIndex, previewMaterialInstance, textureCenterMatrix));
                        } else {
                            Log.Debug?.Error($"Preview material for {nameof(TopDownDepthTexturesLoadingManager)} is not set");
                        }
#endif
                    }
                } else {
                    Log.Important?.Error($"Failed to load chunk {chunkState.chunkIndex}. Texture {chunkState.chunkIndex} was not in {nameof(_loadingFromDiskDatas)} list");
                    cancel = true;
                }
            } catch (Exception e) {
                Log.Important?.Error($"Failed to load chunk {chunkState.chunkIndex}. Exception below");
                Debug.LogException(e);
                cancel = true;
            }
        }

        void StartLoadingToGpu(int chunkIndex, UnsafeArray<byte> textureData, out bool failed, out short layerWhereLoading) {
            failed = false;
            layerWhereLoading = (short)mathExt.IndexOf(chunkIndex, _reservedToLoadToGpuChunks);
            if (layerWhereLoading == -1) {
                layerWhereLoading = GetFreeTexturesArrayLayer();
            }
            if (layerWhereLoading == -1) {
                Log.Important?.Error("No free layer for new texture.");
                failed = true;
                return;
            }
            try {
                PrepareComputeShaderCommandsAndBuffer(textureData.LengthInt, layerWhereLoading, out var commands, out var textureDataBuffer);
                var data = new TextureLoadToGpuData(chunkIndex, layerWhereLoading, commands, textureDataBuffer, textureData);
                _loadingToGpuDatas.Add(data);
            } catch (Exception e) {
                Debug.LogException(e);
                failed = true;
            }
        }

        void PrepareComputeShaderCommandsAndBuffer(int textureDataLength, int layer, out CommandBuffer commands, out ComputeBuffer textureDataBuffer) {
            using var marker = DispatchComputeShaderMarker.Auto();
            commands = new CommandBuffer();
            commands.name = nameof(TopDownDepthTexturesLoadingManager) + "_CopyTextureToTexturesArray";
            commands.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

            int kernelIndex = wetnessTexturesArrayDataSetShader.FindKernel(CopyDataBufferToTexturesArrayKernelName);

            commands.SetComputeTextureParam(wetnessTexturesArrayDataSetShader, kernelIndex, TexturesArrayPropId, DepthTexturesArray);

            commands.SetComputeFloatParam(wetnessTexturesArrayDataSetShader, TextureSizePropId, ChunkTextureSize);
            commands.SetComputeFloatParam(wetnessTexturesArrayDataSetShader, ToLayerPropId, layer);

            textureDataBuffer = new ComputeBuffer(textureDataLength / sizeof(float), sizeof(float));
            commands.SetComputeBufferParam(wetnessTexturesArrayDataSetShader, kernelIndex, DataBufferPropId, textureDataBuffer);

            wetnessTexturesArrayDataSetShader.GetKernelThreadGroupSizes(kernelIndex, out var groupsCountX, out var groupsCountY, out _);
            commands.DispatchCompute(wetnessTexturesArrayDataSetShader, kernelIndex,
                Mathf.CeilToInt((float)ChunkTextureSize / groupsCountX), Mathf.CeilToInt((float)ChunkTextureSize / groupsCountY), 1);
        }

        void CopyTextureDataPartToGpuBuffer(TextureLoadToGpuData data, out int bytesCopied) {
            if (data.copiedBytesCount >= data.textureData.Length) {
                bytesCopied = 0;
                return;
            }
            var bytesCountRemainingToCopy = data.textureData.LengthInt - data.copiedBytesCount;
            if (bytesCountRemainingToCopy <= 0) {
                bytesCopied = 0;
                return;
            }
            bytesCopied = math.min(bytesCountRemainingToCopy, _textureDataMaxBytesToCopyPerFrame);
            using var marker = CopyTextureDataPartMarker.Auto();
            data.textureDataGpuBuffer.SetData(data.textureData.AsNativeArray(), data.copiedBytesCount, data.copiedBytesCount, bytesCopied);
        }

        void ProcessLoadingChunkToGpu(ChunkIndexWithLoadingState chunkState, out bool cancel, out bool loaded, out int layerWhereLoaded, ref bool executedExpensiveOperation) {
            cancel = false;
            loaded = false;
            layerWhereLoaded = -1;
            try {
                if (TryGetIndexOfLoadingToGpuData(chunkState.chunkIndex, _loadingToGpuDatas, out var indexInArr)) {
                    ref TextureLoadToGpuData dataRef = ref _loadingToGpuDatas[indexInArr];
                    if (dataRef.isStartedExecutingComputeShader == false && executedExpensiveOperation == false) {
                        CopyTextureDataPartToGpuBuffer(dataRef, out var bytesCopied);
                        dataRef.copiedBytesCount += bytesCopied;
                        if (bytesCopied == 0) {
                            UnityEngine.Graphics.ExecuteCommandBufferAsync(dataRef.commands, ComputeQueueType.Default);
                            dataRef.fence = AsyncGPUReadback.RequestAsync(dataRef.textureDataGpuBuffer, 1, 0).GetAwaiter();
                            dataRef.DisposeAllExceptGpuBuffer();
                            dataRef.isStartedExecutingComputeShader = true;
                        }
                        executedExpensiveOperation = true;
                    } else if (dataRef.isStartedExecutingComputeShader && dataRef.fence.IsCompleted) {
                        dataRef.DisposeAll();
                        loaded = true;
                        layerWhereLoaded = dataRef.layer;
                        _loadingToGpuDatas.RemoveAtSwapBack(indexInArr);
                    }
                } else {
                    Log.Important?.Error($"Failed to load chunk {chunkState.chunkIndex}. Texture {chunkState.chunkIndex} was not in {nameof(_loadingToGpuDatas)} list");
                    cancel = true;
                }
            } catch (Exception e) {
                Log.Important?.Error($"Failed to load chunk {chunkState.chunkIndex}. Exception below");
                Debug.LogException(e);
                cancel = true;
            }
        }

        void CancelLoadingAll(bool waitForComputeShaderFinishExecuting) {
            if (_loadingChunksStates.IsCreated == false) {
                return;
            }
            for (int i = _loadingChunksStates.Count - 1; i >= 0; i--) {
                CancelLoadingChunkTexture(_loadingChunksStates[i], waitForComputeShaderFinishExecuting);
                _loadingChunksStates.RemoveAtSwapBack(i);
            }
            if (waitForComputeShaderFinishExecuting) {
                for (int i = 0; i < _cancelledWaitingForCleanupLoadingToGpuDatas.Count; i++) {
                    ref var loadingToGpuDataRef = ref _cancelledWaitingForCleanupLoadingToGpuDatas[i];
                    loadingToGpuDataRef.fence.GetResult();
                    loadingToGpuDataRef.DisposeAll();
                }
                _cancelledWaitingForCleanupLoadingToGpuDatas.Clear();
            } else {
                // In late update it will wait for cleanup of loading to gpu datas
                UnityUpdateProvider.GetOrCreate().RegisterLateGeneric(this);
            }
        }

        bool CheckIfCancelledComputeShadersFinishedAndDispose() {
            var disposedAll = true;
            for (int i = _cancelledWaitingForCleanupLoadingToGpuDatas.Count - 1; i >= 0; i--) {
                ref var dataRef = ref _cancelledWaitingForCleanupLoadingToGpuDatas[i];
                if (dataRef.fence.IsCompleted) {
                    dataRef.DisposeAll();
                    _cancelledWaitingForCleanupLoadingToGpuDatas.RemoveAtSwapBack(i);
                } else {
                    disposedAll = false;
                }
            }
            return disposedAll;
        }

        public static string GetTextureFullPath(string mapSceneName, int2 textureCoord) {
            return Path.Combine(GetTexturesDirectory(mapSceneName), $"depth_tex_{textureCoord.x}_{textureCoord.y}.raw");
        }

        public static string GetTexturesDirectory(string mapSceneName) {
            if (mapSceneName == null) {
                Log.Important?.Error("Map scene name is null");
                return "";
            }
            if (s_currentTexturesDirectory.mapSceneName != mapSceneName) {
                s_currentTexturesDirectory.mapSceneName = mapSceneName;
                s_currentTexturesDirectory.directoryPath = Path.Combine(Application.streamingAssetsPath, TexturesDirectoryInStreamingAssets, mapSceneName);
            }
            return s_currentTexturesDirectory.directoryPath;
        }

        void CancelLoadingChunkTexture(ChunkIndexWithLoadingState chunkState, bool waitForComputeShaderFinishExecuting) {
            switch (chunkState.state) {
                case ChunkLoadingState.LoadingFromDisk: {
                    if (TryGetIndexOfLoadingFromDiskData(chunkState.chunkIndex, _loadingFromDiskDatas, out var indexInArr)) {
                        ref var dataRef = ref _loadingFromDiskDatas[indexInArr];
                        dataRef.readHandle.Cancel();
                        dataRef.DisposeAll();
                        _loadingFromDiskDatas.RemoveAtSwapBack(indexInArr);
                    }
                    break;
                }
                case ChunkLoadingState.LoadingToGpuBuffer: {
                    if (TryGetIndexOfLoadingToGpuData(chunkState.chunkIndex, _loadingToGpuDatas, out var indexInArr)) {
                        ref var dataRef = ref _loadingToGpuDatas[indexInArr];
                        if (dataRef.isStartedExecutingComputeShader == false) {
                            var indexInReserved = mathExt.IndexOf(dataRef.chunkIndex, _reservedToLoadToGpuChunks);
                            if (indexInReserved != -1) {
                                _reservedToLoadToGpuChunks[indexInReserved] = -1;
                            }
                            dataRef.DisposeAll();
                        } else {
                            if (waitForComputeShaderFinishExecuting) {
                                dataRef.fence.GetResult();
                                dataRef.DisposeAll();
                            } else {
                                // else we cannot cancel commands which were scheduled to execute. All compute shaders executed on the same queue (here default queue)
                                // will not execute in parallel and will execute in order in which execute was called, so 
                                // executing compute shader with write to the same textures array layer as previously scheduled will not result in race condition
                                _cancelledWaitingForCleanupLoadingToGpuDatas.Add(dataRef);
                            }
                        }
                        _loadingToGpuDatas.RemoveAtSwapBack(indexInArr);
                    }
                    break;
                }
            }
#if UNITY_EDITOR
            EDITOR_DisposeLoadedPreviewTexture(chunkState.chunkIndex);
#endif
        }

        bool TryGetLayerWhereTextureIsLoadedInTexturesArray(int chunkIndex, out int layer) {
            if (chunkIndex == -1) {
                layer = -1;
                return false;
            }
            layer = mathExt.IndexOf(chunkIndex, _loadedToGpuChunks);
            return layer != -1;
        }

        static bool TryGetIndexOfLoadingFromDiskData(int chunkIndex, StructList<TextureLoadFromDiskData> loadingFromDiskDatas, out int indexInArr) {
            int count = loadingFromDiskDatas.Count;
            for (int i = 0; i < count; i++) {
                if (loadingFromDiskDatas[i].chunkIndex == chunkIndex) {
                    indexInArr = i;
                    return true;
                }
            }
            indexInArr = -1;
            return false;
        }

        static bool TryGetIndexOfLoadingChunkState(int chunkIndex, StructList<ChunkIndexWithLoadingState> loadingChunksStates, out int indexInArr) {
            int count = loadingChunksStates.Count;
            for (int i = 0; i < count; i++) {
                if (loadingChunksStates[i].chunkIndex == chunkIndex) {
                    indexInArr = i;
                    return true;
                }
            }
            indexInArr = -1;
            return false;
        }

        static bool TryGetIndexOfLoadingToGpuData(int chunkIndex, StructList<TextureLoadToGpuData> loadingToGpuDatas, out int indexInArr) {
            int count = loadingToGpuDatas.Count;
            for (int i = 0; i < count; i++) {
                if (loadingToGpuDatas[i].chunkIndex == chunkIndex) {
                    indexInArr = i;
                    return true;
                }
            }
            indexInArr = -1;
            return false;
        }

        void GetChunksToLoadMinMax(StretchingRect area, out int2 min, out int2 max) {
            min = (int2)math.floor(area.value.min * _textureSizeInUnitsRcp);
            min = math.clamp(min, new int2(0), _chunksMaxCountXY - 1);
            max = (int2)math.floor(area.value.max * _textureSizeInUnitsRcp);
            max = math.clamp(max, new int2(0), _chunksMaxCountXY - 1);
        }

        void UpdateLoadingAreaRect(ref StretchingRect loadingAreaRect, float2 heroPosInGameBoundsSpace) {
            var loadingAreaRectHalfSize = _chunkTextureSizeInUnits * (math.min(visibleAreaRectScale + preloadChunksScaleAdd, MaxLoadingAreaScale)) * 0.5f;
            var loadingAreaRectMinMax = new MinMaxAABR(
                (heroPosInGameBoundsSpace - new float2(loadingAreaRectHalfSize)),
                (heroPosInGameBoundsSpace + new float2(loadingAreaRectHalfSize)));
            bool isAreaRectValid = math.all(loadingAreaRect.value.max > loadingAreaRectMinMax.min) && math.all(loadingAreaRect.value.min < loadingAreaRectMinMax.max);
            loadingAreaRect = isAreaRectValid ? loadingAreaRect : new(loadingAreaRectMinMax);
            // Ensures that only 4 textures are needed to be loaded at any time
            var unloadChunksDistance = math.max(MaxLoadingAreaScale - visibleAreaRectScale - preloadChunksScaleAdd, 0) * _chunkTextureSizeInUnits;
            loadingAreaRect.Update(loadingAreaRectMinMax, unloadChunksDistance);
        }

        int GetChunkIndex(int2 coord) {
            return GetChunkIndex(coord.x, coord.y);
        }

        int GetChunkIndex(int x, int y) {
            var index = y * _chunksMaxCountXY.x + x;
            index = math.select(index, -1, (index < 0) | (index >= (_chunksMaxCountXY.x * _chunksMaxCountXY.y)));
            return index;
        }

        int2 GetChunkCoord(int index) {
            var y = index / _chunksMaxCountXY.x;
            var x = index % _chunksMaxCountXY.x;
            return new int2(x, y);
        }

        short GetFreeTexturesArrayLayer() {
            return (short)mathExt.IndexOf(-1, _reservedToLoadToGpuChunks);
        }

        struct ChunkIndexWithLoadingState : IComparable<ChunkIndexWithLoadingState> {
            public readonly int chunkIndex;
            public ChunkLoadingState state;

            public ChunkIndexWithLoadingState(int chunkIndex, ChunkLoadingState state) {
                this.chunkIndex = chunkIndex;
                this.state = state;
            }

            public int CompareTo(ChunkIndexWithLoadingState other) {
                return ((byte)state).CompareTo((byte)other.state);
            }
        }

        enum ChunkLoadingState : byte {
            None = 0,
            LoadingFromDisk = 1,
            LoadingToGpuBuffer = 2,
        }

        struct TextureLoadFromDiskData {
            public int chunkIndex;
            public UnsafeArray<byte> textureData;
            public ReadHandle readHandle;
            public UnsafeArray<ReadCommand> commands;

            public TextureLoadFromDiskData(int chunkIndex, UnsafeArray<byte> textureData, ReadHandle readHandle, UnsafeArray<ReadCommand> commands) {
                this.chunkIndex = chunkIndex;
                this.textureData = textureData;
                this.readHandle = readHandle;
                this.commands = commands;
            }

            public void DisposeNotSharedData() {
                readHandle.Dispose();
                readHandle = default;
                commands.Dispose();
                commands = default;
            }

            public void DisposeAll() {
                textureData.Dispose();
                if (readHandle.IsValid()) {
                    readHandle.Dispose();
                }
                if (commands.IsCreated) {
                    commands.Dispose();
                }
            }
        }

        struct TextureLoadToGpuData {
            public readonly int chunkIndex;
            public readonly short layer;
            public UnsafeArray<byte> textureData;
            public CommandBuffer commands;
            public ComputeBuffer textureDataGpuBuffer;
            public Awaitable<AsyncGPUReadbackRequest>.Awaiter fence;
            public int copiedBytesCount;
            public bool isStartedExecutingComputeShader;

            public TextureLoadToGpuData(int chunkIndex, short layer, CommandBuffer commands, ComputeBuffer textureDataGpuBuffer, UnsafeArray<byte> textureData) {
                this.chunkIndex = chunkIndex;
                this.layer = layer;
                this.commands = commands;
                this.textureDataGpuBuffer = textureDataGpuBuffer;
                this.textureData = textureData;
                fence = default;
                copiedBytesCount = 0;
                isStartedExecutingComputeShader = false;
            }

            public void DisposeAllExceptGpuBuffer() {
                commands?.Dispose();
                commands = null;
                if (textureData.IsCreated) {
                    textureData.Dispose();
                }
            }

            public void DisposeAll() {
                commands?.Dispose();
                commands = null;
                textureDataGpuBuffer?.Dispose();
                textureDataGpuBuffer = null;
                if (textureData.IsCreated) {
                    textureData.Dispose();
                }
            }
        }

        struct TexturesDirectoryData {
            public string mapSceneName;
            public string directoryPath;
        }

#if UNITY_EDITOR
        void OnValidate() {
            if (groundBounds == null && Application.isPlaying == false) {
                var allGroundBounds = FindObjectsByType<GroundBounds>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (allGroundBounds.Length == 1) {
                    groundBounds = allGroundBounds[0];
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
            if (_previewData.loadedTexturesPreviewData.IsCreated) {
                for (int i = 0; i < _previewData.loadedTexturesPreviewData.Count; i++) {
                    var previewMaterialInstance = _previewData.loadedTexturesPreviewData[i].previewMaterialInstance;
                    if (previewMaterialInstance == null) {
                        continue;
                    }
                    var color = previewMaterialInstance.color;
                    if (color.a != previewTexturesAlpha) {
                        color.a = previewTexturesAlpha;
                        previewMaterialInstance.color = color;
                    }
                }
            }
        }

        void OnDrawGizmos() {
            if (DepthTexturesArray == null) {
                return;
            }
            if (_previewBounds) {
                Gizmos.color = Color.white;
                if (Hero.Current != null) {
                    var heroPos2d = Hero.Current.Coords.XZ();
                    var loadingRectSize = _chunkTextureSizeInUnits * visibleAreaRectScale;
                    Gizmos.DrawWireCube(new Vector3(heroPos2d.x, _previewData.gameBoundsMaxY, heroPos2d.y), new Vector3(loadingRectSize, 0, loadingRectSize));
                }

                GetChunksToLoadMinMax(_loadingAreaRect, out var chunksToLoadAreaMinCoord, out var chunksToLoadAreaMaxCoord);

                for (int y = chunksToLoadAreaMinCoord.y; y <= chunksToLoadAreaMaxCoord.y; y++) {
                    for (int x = chunksToLoadAreaMinCoord.x; x <= chunksToLoadAreaMaxCoord.x; x++) {
                        EDITOR_DrawChunkBounds(GetChunkIndex(x, y));
                    }
                }
                EDITOR_DrawAABR(_gameBounds2d.min, _loadingAreaRect.value, _previewData.gameBoundsMaxY, Color.blue);
                EDITOR_DrawAABR(_gameBounds2d.min, _visibleAreaRect, _previewData.gameBoundsMaxY, Color.cyan);
            }
        }

        void EDITOR_DrawChunkBounds(int chunkIndex) {
            if (chunkIndex == -1 || _chunksValidStatuses[(uint)chunkIndex] == false) {
                return;
            }
            var coord = GetChunkCoord(chunkIndex);
            var textureMinCornerPos = _gameBounds2d.min + ((float2)coord * _chunkTextureSizeInUnits);
            var textureHalfSize = _chunkTextureSizeInUnits * 0.5f;
            var textureCenterPos = new float3(textureMinCornerPos.x, 0, textureMinCornerPos.y) + new float3(textureHalfSize, 0, textureHalfSize);
            var textureBoundsSize = new float3(_chunkTextureSizeInUnits, 0.01f, _chunkTextureSizeInUnits);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(textureCenterPos, textureBoundsSize);
            const float colorTransparency = 0.5f;
            if (TryGetIndexOfLoadingChunkState(chunkIndex, _loadingChunksStates, out var indexInArr)) {
                var chunkState = _loadingChunksStates[indexInArr].state;
                var color = chunkState switch {
                    ChunkLoadingState.None => Color.red,
                    ChunkLoadingState.LoadingFromDisk => Color.blue,
                    ChunkLoadingState.LoadingToGpuBuffer => Color.cyan,
                    _ => Color.magenta
                };
                color.a = colorTransparency;
                Gizmos.color = color;
            } else {
                Color color;
                if (TryGetLayerWhereTextureIsLoadedInTexturesArray(chunkIndex, out _)) {
                    color = Color.green;
                    color.a = colorTransparency;
                } else {
                    color = new Color(0, 0, 0, 0);
                }
                Gizmos.color = color;
            }
            Gizmos.DrawCube(textureCenterPos, textureBoundsSize);
        }

        static void EDITOR_DrawAABR(float2 worldOffset, MinMaxAABR rect, float rectY, Color color) {
            var worldRectMin = worldOffset + (rect.min);
            var worldRectMax = worldOffset + (rect.max);
            var worldRectCenter = math.lerp(worldRectMin, worldRectMax, 0.5f);
            var worldRectSize = worldRectMax - worldRectMin;
            Gizmos.color = color;
            Gizmos.DrawWireCube(new Vector3(worldRectCenter.x, rectY, worldRectCenter.y), new Vector3(worldRectSize.x, 0, worldRectSize.y));
        }

        void EDITOR_DisposeLoadedPreviewTexture(int chunkIndex) {
            for (int i = 0; i < _previewData.loadedTexturesPreviewData.Count; i++) {
                var previewData = _previewData.loadedTexturesPreviewData[i];
                if (previewData.chunkIndex == chunkIndex) {
                    if (previewData.previewMaterialInstance != null) {
                        if (previewData.previewMaterialInstance.mainTexture != null) {
                            Destroy(previewData.previewMaterialInstance.mainTexture);
                        }
                        Destroy(previewData.previewMaterialInstance);
                    }
                    _previewData.loadedTexturesPreviewData.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        static Mesh EDITOR_CreateQuadMesh() {
            var mesh = new Mesh();

            Vector3[] vertices = {
                new(-0.5f, 0, -0.5f),
                new(0.5f, 0, -0.5f),
                new(-0.5f, 0, 0.5f),
                new(0.5f, 0, 0.5f)
            };

            int[] triangles = {
                0, 2, 1,
                2, 3, 1
            };
            Vector2[] uv = {
                new(0, 0),
                new(1, 0),
                new(0, 1),
                new(1, 1)
            };

            Vector3[] normals = {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.normals = normals;

            return mesh;
        }

        struct EditorPreviewData {
            public float gameBoundsMaxY;
            public StructList<TexturePreviewData> loadedTexturesPreviewData;
            public Mesh quadMesh;
            public Material previewMaterial;

            public EditorPreviewData(int preAllocCount) {
                loadedTexturesPreviewData = new StructList<TexturePreviewData>(preAllocCount);
                gameBoundsMaxY = 0;
                quadMesh = null;
                previewMaterial = null;
            }
        }

        struct TexturePreviewData {
            public int chunkIndex;
            public Material previewMaterialInstance;
            public Matrix4x4 matrix;

            public TexturePreviewData(int chunkIndex, Material previewMaterialInstance, Matrix4x4 matrix) {
                this.chunkIndex = chunkIndex;
                this.previewMaterialInstance = previewMaterialInstance;
                this.matrix = matrix;
            }
        }
#endif
    }
}
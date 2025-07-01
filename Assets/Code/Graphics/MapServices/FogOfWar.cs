using System;
using System.Runtime.CompilerServices;
using Awaken.TG.Assets;
using Awaken.TG.Main.FastTravel;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Map;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Graphics.MapServices {
    [BurstCompile]
    public class FogOfWar {
        const int PreallocateNodesCount = 256;
        const int PreallocateDatasCount = 128;
        static readonly int ConstantsPropId = Shader.PropertyToID("Constants");
        static readonly int VisibleMapMaskPropId = Shader.PropertyToID("VisibleMapMask");
        static readonly int VisitedPixelsPropId = Shader.PropertyToID("VisitedPixels");

        public SceneReference Scene { get; private set; }
        readonly MapMemory _memory;
        
        UnsafeInsertOnlyQuadtree<float2> _visitedPixelsQuadtree;
        IEventListener _onDomainChangedListener;
        IEventListener _onPlayerMovedListener;
        ComputeShader _fogOfWarShader;
        SpriteReference _mapSpriteRef;
        Rect _mapBoundsRect;
        int2 _maskSize;
        int _revealPixelsRadius;
        float _revealBrushIntensity;
        int _softAreaPixelsCount;
        float _falloffPow;
        float _quadtreeCircleRadius;
        float _breakDistanceInCirclesBetween;
        float _mapTextureRectSizeXInv;
        bool _isParamsLoaded;
        bool _memoryRead;
        bool _isFromActiveScene;

        public bool IsInitialized() => _isParamsLoaded;
        bool HasMap => _mapSpriteRef != null;
        bool IsValidAndInitialized => _maskSize.Equals(default) == false && _isParamsLoaded;

        public FogOfWar(SceneReference scene, MapMemory memory) {
            Scene = scene;
            _memory = memory;
            InitializeParams();
            _onPlayerMovedListener = Hero.Current.ListenTo(GroundedEvents.AfterMovedToPosition, OnPlayerMoved);
            _onDomainChangedListener = World.EventSystem.ListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.AfterNewDomainSet, OnNewDomainSet);
            OnNewDomainSet(World.Services.Get<SceneService>().ActiveSceneRef);
        }

        public void Dispose() {
            _isParamsLoaded = false;
            _visitedPixelsQuadtree.Dispose();
            _mapSpriteRef?.Release();
            _mapSpriteRef = null;
            World.EventSystem.TryDisposeListener(ref _onPlayerMovedListener);
            World.EventSystem.TryDisposeListener(ref _onDomainChangedListener);
        }

        public void WriteMemory() {
            if (!_memoryRead || !_isParamsLoaded) {
                return;
            }
            var datasCount = _visitedPixelsQuadtree.DatasCount;
            if (_memory.visitedPixels.Length != datasCount) {
                _memory.visitedPixels = new float2[datasCount];
            }
            _visitedPixelsQuadtree.CopyDatasToArray(_memory.visitedPixels);
        }

        public void ReadMemory() {
            _memoryRead = true;
            if (_isParamsLoaded) {
                InitializeQuadtreeFromVisitedPixels();
            }
        }

        public bool IsPositionRevealed(Vector3 worldPosition) {
            if (!HasMap) {
                return true;
            }

            if (!IsValidAndInitialized) {
                Log.Important?.Error($"Trying to Call {nameof(IsPositionRevealed)} when {nameof(FogOfWar)} is not valid or not initialized");
                return false;
            }

            var normalizedPixelPos = WorldPositionToNormalizedMapPosition(worldPosition);
            var pixelPos = NormalizedPixelToPixel(normalizedPixelPos);
            return _visitedPixelsQuadtree.IsOverlappingAny(pixelPos, _quadtreeCircleRadius);
        }

        [UnityEngine.Scripting.Preserve]
        public bool IsInCurrentViewRadius(Vector3 worldPosition) {
            if (!HasMap) {
                return true;
            }

            if (!IsValidAndInitialized) {
                Log.Important?.Error($"Trying to Call {nameof(IsInCurrentViewRadius)} when {nameof(FogOfWar)} is not valid or not initialized");
                return false;
            }

            var hero = Hero.Current;
            if (hero == null) {
                return false;
            }

            var pixelPos = NormalizedPixelToPixel(WorldPositionToNormalizedMapPosition(worldPosition));
            var currentPositionPixelPos = NormalizedPixelToPixel(WorldPositionToNormalizedMapPosition(hero.Coords));
            var distanceSq = math.distancesq(pixelPos, currentPositionPixelPos);
            return distanceSq < _revealPixelsRadius * _revealPixelsRadius;
        }

        public RenderTexture CreateMaskTexture() {
            if (!HasMap) {
                return null;
            }

            if (!IsValidAndInitialized) {
                Log.Important?.Error($"Trying to get FoW mask texture when {nameof(FogOfWar)} service in not valid or not initialized");
                return null;
            }

            RenderTexture visibleMapMask = CreateEmptyVisibleMapMask(_maskSize.x, _maskSize.y);
            int pixelsCount = VisitedPixelsCount();
            if (pixelsCount == 0) {
                return visibleMapMask;
            }
            
            // Break visible area line if gap between adjacent revealed area circles is greater than
            // then size of one reveal area circle (If between two adjacent revealed area circles can fit
            // more than one area circle - insert break)
            var circleSize = _revealPixelsRadius * 2;
            float breakDistanceSq = math.square(_breakDistanceInCirclesBetween * circleSize + circleSize);
            var visitedPixelNormWithBreaks = new UnsafeList<float2>(pixelsCount + 10, ARAlloc.Temp);
            Optional<float2> currentPixelNorm = (_isFromActiveScene && Hero.Current != null) ? Optional<float2>.Some(WorldPositionToNormalizedMapPosition(Hero.Current.Coords)) : Optional<float2>.None;
            unsafe {
                GenerateVisitedPixelsList(in breakDistanceSq, _visitedPixelsQuadtree.DatasPtr, in pixelsCount,
                    in _maskSize, in _mapTextureRectSizeXInv, in currentPixelNorm, ref visitedPixelNormWithBreaks);
            }

            RevealVisitedPixels(_fogOfWarShader, _revealPixelsRadius, _revealBrushIntensity,
                _softAreaPixelsCount, _falloffPow, visitedPixelNormWithBreaks, currentPixelNorm, ref visibleMapMask);

            visitedPixelNormWithBreaks.Dispose();
            return visibleMapMask;
        }

        void OnNewDomainSet(SceneReference activeScene) {
            _isFromActiveScene = Scene.Equals(activeScene);
        }

        void OnPlayerMoved(Vector3 position) {
            if (!IsValidAndInitialized) {
                return;
            }

            if (!_isFromActiveScene) {
                return;
            }

            float2 newPixelNorm = WorldPositionToNormalizedMapPosition(position);
            if (math.any(newPixelNorm < new float2(0f, 0f)) | math.any(newPixelNorm > new float2(1f, 1f))) {
                return;
            }

            float2 pixel = NormalizedPixelToPixel(newPixelNorm);
            bool isOverlappingAny = _visitedPixelsQuadtree.IsOverlappingAny(pixel, _quadtreeCircleRadius);
            if (!isOverlappingAny) {
                _visitedPixelsQuadtree.Insert(newPixelNorm, pixel);
            }
        }

        void InitializeParams() {
            try {
                _maskSize = default;
                _mapSpriteRef = null;
                _isParamsLoaded = false;

                if (!CommonReferences.Get.MapData.byScene.TryGetValue(Scene, out var mapData)) {
                    _isParamsLoaded = true;
                    return;
                }
                _mapSpriteRef = mapData.Sprite.Get();
                if (_mapSpriteRef == null || _mapSpriteRef.IsSet == false) {
                    _mapSpriteRef = null;
                    _isParamsLoaded = true;
                    return;
                }

                _mapBoundsRect = GetMapBounds(mapData);

                var gameConstants = GameConstants.Get;
                _fogOfWarShader = gameConstants.fogOfWarShader;
                var fogOfWarParams = gameConstants.fogOfWarParams;
                _breakDistanceInCirclesBetween = fogOfWarParams.breakDistanceInCirclesBetween;
                _revealPixelsRadius = GetScaledPixelParameter(fogOfWarParams.revealPixelsRadius, fogOfWarParams.maskTextureMultiplier);
                _softAreaPixelsCount = GetScaledPixelParameter(fogOfWarParams.softAreaPixelsCount, fogOfWarParams.maskTextureMultiplier);
                _falloffPow = fogOfWarParams.falloffPow;
                _quadtreeCircleRadius = GetScaledPixelParameter(_revealPixelsRadius, fogOfWarParams.revealRadiusThreshold * 0.5f);
                _visitedPixelsQuadtree = new UnsafeInsertOnlyQuadtree<float2>(default, _quadtreeCircleRadius, 0.9f, PreallocateNodesCount, PreallocateDatasCount, ARAlloc.Persistent);
                _revealBrushIntensity = fogOfWarParams.revealBrushIntensity;
                _mapSpriteRef.arSpriteReference.LoadAsset<Sprite>().OnComplete(OnMapSpriteLoaded);
            } catch (Exception e) {
                Debug.LogException(e);
                _isParamsLoaded = true;
            }
        }

        void OnMapSpriteLoaded(ARAsyncOperationHandle<Sprite> mapSpriteHandle) {
            if (_mapSpriteRef == null) {
                return;
            }

            _isParamsLoaded = true;
            var mapSprite = mapSpriteHandle.Result;
            if (mapSprite == null) {
                Log.Important?.Error($"Map sprite loading failed");
                _visitedPixelsQuadtree.Dispose();
                return;
            }

            var mapTexture = mapSprite.texture;
            int2 mapSize;
            mapSize.x = mapTexture.width;
            mapSize.y = mapTexture.height;
            _mapSpriteRef.Release();

            _maskSize = (int2)math.ceil((float2)mapSize * GameConstants.Get.fogOfWarParams.maskTextureMultiplier);
            var mapTextureRect = mapSprite.textureRect;
            _mapTextureRectSizeXInv = math.rcp(mapTextureRect.width);
            _visitedPixelsQuadtree.Rect = new Rect(Vector2.zero, (float2)_maskSize);
            if (_memoryRead) {
                InitializeQuadtreeFromVisitedPixels();
            }

            OnPlayerMoved(Hero.Current.Coords);
        }

        static Rect GetMapBounds(MapSceneData data) {
            var boundsSize = data.Bounds.size.xz();
            var margin = VMapCamera.MarginSize * VMapCamera.MarginSizeWorldMultiplier;
            var newBoundsSize = boundsSize * margin;
            newBoundsSize.x = newBoundsSize.y * data.AspectRatio;
            var boundsCenter = data.Bounds.center.xz();
            var boundsHalfExtent = newBoundsSize * 0.5f;
            var newBoundsMin = boundsCenter - boundsHalfExtent;
            var newBoundsMax = boundsCenter + boundsHalfExtent;
            return new Rect(newBoundsMin, newBoundsMax - newBoundsMin);
        }

        void InitializeQuadtreeFromVisitedPixels() {
            for (int i = 0; i < _memory.visitedPixels.Length; i++) {
                var pixelNorm = _memory.visitedPixels[i];
                var pixel = NormalizedPixelToPixel(pixelNorm);
                _visitedPixelsQuadtree.Insert(pixelNorm, pixel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int VisitedPixelsCount() => _visitedPixelsQuadtree.DatasCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float2 WorldPositionToNormalizedMapPosition(Vector3 position) => WorldPositionToNormalizedMapPosition(position, _mapBoundsRect);

        [BurstCompile]
        static unsafe void GenerateVisitedPixelsList(in float breakDistanceSq, float2* visitedPixelsPtr, in int visitedPixelsCount,
            in int2 maskSize, in float mapTextureRectSizeXInv, in Optional<float2> currentPixelNorm,
            ref UnsafeList<float2> visitedPixelNormWithBreaks) {
            var firstPixelNorm = visitedPixelsPtr[0];
            visitedPixelNormWithBreaks.Add(firstPixelNorm);

            for (int i = 1; i < visitedPixelsCount; i++) {
                var pixelNorm = visitedPixelsPtr[i];
                var prevPixelNorm = visitedPixelsPtr[i - 1];
                var pixel = NormalizedPixelToPixel(in pixelNorm, in maskSize);
                var prevPixel = NormalizedPixelToPixel(in prevPixelNorm, in maskSize);
                bool isPathBreak = IsPathBreak(prevPixel, pixel, breakDistanceSq);
                if (Hint.Unlikely(isPathBreak)) {
                    // Compute Shader operates on pairs of visited pixels, so if
                    // currently inserting path break, ensure that the pixel before that
                    // path break has a visited pixel to pair with
                    bool isPrevPixelWithoutPair = i == 1 || visitedPixelNormWithBreaks[^2].x == -1;
                    if (Hint.Unlikely(isPrevPixelWithoutPair)) {
                        visitedPixelNormWithBreaks.Add(prevPixelNorm + GetMinimalPixelOffset(in prevPixelNorm, in mapTextureRectSizeXInv));
                    }

                    visitedPixelNormWithBreaks.Add(new float2(-1f, -1f));
                }

                visitedPixelNormWithBreaks.Add(pixelNorm);
            }

            var lastPixel = visitedPixelNormWithBreaks[^1];
            if (currentPixelNorm.HasValue && math.any(math.abs(currentPixelNorm.Value - lastPixel) > 0.001f)) {
                visitedPixelNormWithBreaks.Add(currentPixelNorm.Value);
            }

            bool isLastPixelWithoutPair = visitedPixelNormWithBreaks.Length < 2 || visitedPixelNormWithBreaks[^2].x == -1f;
            // If prev pixel is path break or this is first pixel, it is needed to insert temporary pixel to
            // make a pair of visited pixels because compute shader operates on pairs of pixels and
            // ignores any pair with a break pixel or list of length 1
            if (Hint.Unlikely(isLastPixelWithoutPair)) {
                var lastPixelNorm = visitedPixelNormWithBreaks[^1];
                visitedPixelNormWithBreaks.Add(lastPixelNorm + GetMinimalPixelOffset(lastPixelNorm, mapTextureRectSizeXInv));
            }
        }

        static void RevealVisitedPixels(ComputeShader fogOfWarShader, int revealPixelsRadius, float revealBrushIntensity,
            int softAreaPixelsCount, float falloffPow,
            GraphicsBuffer visitedPixelsBuffer, Optional<float2> currentPositionPixelNorm, int visitedPixelsCount, ref RenderTexture visibleMapMask) {
            if (visitedPixelsCount == 0) {
                return;
            }

            var constantsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Constant, 1, UnsafeUtility.SizeOf<ShaderConstants>());
            
            var commands = new CommandBuffer();
            commands.name = "FogOfWar";
            commands.SetRenderTarget(visibleMapMask);
            commands.ClearRenderTarget(true, true, Color.black);
            
            commands.SetBufferData(constantsBuffer, new[] {
                new ShaderConstants((uint)visitedPixelsCount - 1, (uint)visibleMapMask.width, (uint)visibleMapMask.height, (uint)revealPixelsRadius,
                    revealBrushIntensity, currentPositionPixelNorm.GetValueOrDefault(), softAreaPixelsCount, 1f / softAreaPixelsCount, falloffPow)
            });
            commands.SetComputeConstantBufferParam(fogOfWarShader, ConstantsPropId, constantsBuffer, 0, UnsafeUtility.SizeOf<ShaderConstants>());

            {
                int revealMapSoftKernelIndex = fogOfWarShader.FindKernel("RevealMapSoft");
                fogOfWarShader.GetKernelThreadGroupSizes(revealMapSoftKernelIndex, out var groupsCountX, out _, out _);
                
                commands.SetComputeTextureParam(fogOfWarShader, revealMapSoftKernelIndex, VisibleMapMaskPropId, visibleMapMask);
                commands.SetComputeBufferParam(fogOfWarShader, revealMapSoftKernelIndex, VisitedPixelsPropId, visitedPixelsBuffer);
                commands.DispatchCompute(fogOfWarShader, revealMapSoftKernelIndex, Mathf.CeilToInt(visitedPixelsCount / (float)groupsCountX), 1, 1);
            }
            
            if (currentPositionPixelNorm.HasValue) {
                int highlightCurrentPositionKernelIndex = fogOfWarShader.FindKernel("HighlightCurrentPosition");
                
                commands.SetComputeTextureParam(fogOfWarShader, highlightCurrentPositionKernelIndex, VisibleMapMaskPropId, visibleMapMask);
                commands.DispatchCompute(fogOfWarShader, highlightCurrentPositionKernelIndex, 1, 1, 1);
            }
            
            UnityEngine.Graphics.ExecuteCommandBuffer(commands);
            commands.Dispose();

            constantsBuffer.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetScaledPixelParameter(int sizeInPixels, float multiplier) {
            return math.max((int)math.ceil(sizeInPixels * multiplier), 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float2 GetMinimalPixelOffset(in float2 lastPixelNorm, in float mapTextureRectSizeXInv) {
            float offset = lastPixelNorm.x < 0.5f ? mapTextureRectSizeXInv : -mapTextureRectSizeXInv;
            return new float2(offset, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int2 NormalizedPixelToPixel(in float2 newPixelPosNormalized) {
            return NormalizedPixelToPixel(in newPixelPosNormalized, _maskSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int2 NormalizedPixelToPixel(in float2 newPixelPosNormalized, in int2 maskSize) {
            return (int2)math.round(newPixelPosNormalized * maskSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsPathBreak(int2 prevPixel, int2 newPixel, float breakDistanceSq) {
            var distanceSq = math.distancesq(new float2(prevPixel.x, prevPixel.y),
                new float2(newPixel.x, newPixel.y));
            return distanceSq > breakDistanceSq;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float2 WorldPositionToNormalizedMapPosition(Vector3 position, Rect mapBoundsRect) {
            float positionXPercent = math.unlerp(mapBoundsRect.min.x, mapBoundsRect.max.x, position.x);
            float positionZPercent = math.unlerp(mapBoundsRect.min.y, mapBoundsRect.max.y, position.z);
            return new float2(positionXPercent, positionZPercent);
        }

        static void RevealVisitedPixels(ComputeShader fogOfWarShader, int revealPixelsRadius, float revealBrushIntensity,
            int softAreaPixelsCount, float falloffPow,
            UnsafeList<float2> visitedPixels, Optional<float2> currentPositionPixelNorm, ref RenderTexture visibleMapMask) {
            var visitedPixelsCount = visitedPixels.Length;
            if (visitedPixelsCount == 0) {
                return;
            }

            var visitedPixelsBuffer = GetVisitedPixelsBuffer(visitedPixels);
            RevealVisitedPixels(fogOfWarShader, revealPixelsRadius, revealBrushIntensity, softAreaPixelsCount, falloffPow,
                visitedPixelsBuffer, currentPositionPixelNorm, visitedPixelsCount, ref visibleMapMask);
            visitedPixelsBuffer.Dispose();
        }

        static RenderTexture CreateEmptyVisibleMapMask(int width, int height) {
            var visibleMapMask = new RenderTexture(width, height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear) {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear
            };
            visibleMapMask.Create();
            return visibleMapMask;
        }

        static GraphicsBuffer GetVisitedPixelsBuffer(UnsafeList<float2> visitedPixels) {
            var visitedPixelsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, visitedPixels.Length, UnsafeUtility.SizeOf<float2>());
            visitedPixelsBuffer.SetData(visitedPixels.AsNativeArray());
            return visitedPixelsBuffer;
        }

        struct ShaderConstants {
            // ReSharper disable once NotAccessedField.Local
            public uint visitedPixelsCountMinusOne;

            // ReSharper disable once NotAccessedField.Local
            public uint textureSizeX;

            // ReSharper disable once NotAccessedField.Local
            public uint textureSizeY;

            // ReSharper disable once NotAccessedField.Local
            public uint revealBrushRadius;

            // ReSharper disable once NotAccessedField.Local
            public float revealBrushIntensity;

            // ReSharper disable once NotAccessedField.Local
            public float2 currentPositionPixelNorm;

            // ReSharper disable once NotAccessedField.Local
            public int softAreaPixelsCount;

            // ReSharper disable once NotAccessedField.Local
            public float invSoftAreaPixelsCount;

            // ReSharper disable once NotAccessedField.Local
            public float falloffPow;

            public ShaderConstants(uint visitedPixelsCountMinusOne, uint textureSizeX, uint textureSizeY, uint revealBrushRadius, float revealBrushIntensity, float2 currentPositionPixelNorm, int softAreaPixelsCount, float invSoftAreaPixelsCount, float falloffPow) {
                this.visitedPixelsCountMinusOne = visitedPixelsCountMinusOne;
                this.textureSizeX = textureSizeX;
                this.textureSizeY = textureSizeY;
                this.revealBrushRadius = revealBrushRadius;
                this.revealBrushIntensity = revealBrushIntensity;
                this.currentPositionPixelNorm = currentPositionPixelNorm;
                this.softAreaPixelsCount = softAreaPixelsCount;
                this.invSoftAreaPixelsCount = invSoftAreaPixelsCount;
                this.falloffPow = falloffPow;
            }
        }
    }
}
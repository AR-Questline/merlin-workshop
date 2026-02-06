using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Awaken.TG.Assets.ShadersPreloading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Awaken.TG.Main.UI.TitleScreen.ShadersPreloading {
    [SpawnsView(typeof(VShadersPreloadingPanel))]
    public partial class ShadersPreloadingPanel : Element<TitleScreenUI> {
        const int DefaultPreloadVariantsPerFrameCount = 20;
        const int DefaultMaxPreloadTimeForCollection = 25;
        const string PreloadVariantsPerFrameCountConfigName = "preload_shader_variants_per_frame_count";
        const string MaxPreloadTimeForCollectionConfigName = "shader_variants_collection_max_preload_time";
        const string ForceSyncPreloadIfExceededTimeConfigName = "shader_variants_collection_force_sync_preload_if_exceeded_time";

        public sealed override bool IsNotSaved => true;

        ShaderVariantCollection[] _shaderVariantCollections;
        GraphicsStateCollection[] _graphicsStateCollections;

        readonly int _allItemsToPrewarmCount;
        readonly int _prewarmPerFrameCount;
        readonly float _maxPreloadTimeForCollection;
        readonly bool _forceSyncPreloadIfExceededTime;

        public new static class Events {
            public static readonly Event<ShadersPreloadingPanel, float> ProgressChanged = new(nameof(ProgressChanged));
        }

        public ShadersPreloadingPanel() {
            _prewarmPerFrameCount = Configuration.GetInt(PreloadVariantsPerFrameCountConfigName, DefaultPreloadVariantsPerFrameCount);
            _maxPreloadTimeForCollection = Configuration.GetFloat(MaxPreloadTimeForCollectionConfigName, DefaultMaxPreloadTimeForCollection);
            _forceSyncPreloadIfExceededTime = Configuration.GetBool(ForceSyncPreloadIfExceededTimeConfigName, true);

            _shaderVariantCollections = ShadersPreloader.TryGetShaderVariantCollectionsToPreload();
            _graphicsStateCollections = ShadersPreloader.TryGetGraphicsStateCollectionsToPreload();

            _allItemsToPrewarmCount = 0;
            foreach (var collection in _shaderVariantCollections) {
                _allItemsToPrewarmCount += collection.variantCount;
            }

            foreach (var collection in _graphicsStateCollections) {
                _allItemsToPrewarmCount += collection.totalGraphicsStateCount;
            }

            if (_allItemsToPrewarmCount != 0) {
                var totalPrewarmCount = math.max(math.ceil(_allItemsToPrewarmCount / (float)_prewarmPerFrameCount), 1);
                Log.Important?.Info($"Preloading progressively shaders variants collection. Warmup frames count: {totalPrewarmCount}");
            } else {
                _shaderVariantCollections = null;
            }
        }

        protected override void OnFullyInitialized() {
            PrewarmRoutine().Forget();
        }

        async UniTaskVoid PrewarmRoutine() {
            await UniTask.NextFrame();

            var completedWarmups = 0;
            var collectionBatchesWarmupTimes = new List<float>();
            var sb = new StringBuilder(64);
            foreach (var variantCollection in _shaderVariantCollections) {
                collectionBatchesWarmupTimes.Clear();
                int lastWarmedUpVariantCount = 0;
                float currentCollectionPreloadStartTime = Time.realtimeSinceStartup;
                float lastWarmupStartTime = Time.realtimeSinceStartup;
                
                while (!variantCollection.isWarmedUp) {
                    if (Time.realtimeSinceStartup - currentCollectionPreloadStartTime > _maxPreloadTimeForCollection) {
                        var currentProgress = GetProgress(completedWarmups + variantCollection.warmedUpVariantCount);
                        
                        sb.Length = 0;
                        AppendWarmupTimesToStringBuilder(sb, collectionBatchesWarmupTimes);

                        Debug.LogException(new Exception($"ShaderVariantCollection {variantCollection.name} async preWarming took longer than {_maxPreloadTimeForCollection} seconds. Current global progress = {currentProgress * 100}%. Prewarm collection synchronously = {_forceSyncPreloadIfExceededTime}. Prewarm per frame count = {_prewarmPerFrameCount}. Collection batches warmup times: {sb}"));
                        if (_forceSyncPreloadIfExceededTime) {
                            variantCollection.WarmUp();
                            await UniTask.NextFrame();
                        }
                        break;
                    }
                    
                    var warmedUpVariantsCount = variantCollection.warmedUpVariantCount;
                    variantCollection.WarmUpProgressively(_prewarmPerFrameCount);
                    await UniTask.NextFrame();
                    
                    if (variantCollection.warmedUpVariantCount != lastWarmedUpVariantCount) {
                        collectionBatchesWarmupTimes.Add(Time.realtimeSinceStartup - lastWarmupStartTime);
                        lastWarmedUpVariantCount = variantCollection.warmedUpVariantCount;
                        lastWarmupStartTime = Time.realtimeSinceStartup;
                    }
                    
                    ReportProgress(completedWarmups + variantCollection.warmedUpVariantCount);
                    if (warmedUpVariantsCount == variantCollection.warmedUpVariantCount) {
                        // Sometimes WarmUpProgressively lefts some items unprocessed, so we call WarmUp to process them
                        variantCollection.WarmUp();
                        await UniTask.NextFrame();
                        break;
                    }
                }

                completedWarmups += variantCollection.variantCount;
                ReportProgress(completedWarmups);

                await Resources.UnloadUnusedAssets();
                GCCleanup();
            }

            foreach (var graphicsStateCollection in _graphicsStateCollections) {
                collectionBatchesWarmupTimes.Clear();
                int lastWarmedUpVariantCount = 0;
                float currentCollectionPreloadStartTime = Time.realtimeSinceStartup;
                float lastWarmupStartTime = Time.realtimeSinceStartup;

                while (!graphicsStateCollection.isWarmedUp) {
                    if (Time.realtimeSinceStartup - currentCollectionPreloadStartTime > _maxPreloadTimeForCollection) {
                        var currentProgress = GetProgress(completedWarmups + graphicsStateCollection.completedWarmupCount);
                        
                        sb.Length = 0;
                        AppendWarmupTimesToStringBuilder(sb, collectionBatchesWarmupTimes);
                        
                        Debug.LogException(new Exception($"GraphicsStateCollection {graphicsStateCollection.name} async preWarming took longer than {_maxPreloadTimeForCollection} seconds. Current global progress = {currentProgress * 100}%. Prewarm collection synchronously = {_forceSyncPreloadIfExceededTime}. Prewarm per frame count = {_prewarmPerFrameCount}. Collection batches warmup times: {sb}"));
                        if (_forceSyncPreloadIfExceededTime) {
                            var finalWarmupJob = graphicsStateCollection.WarmUp();
                            await UniTask.NextFrame();
                            finalWarmupJob.Complete();
                        }
                        break;
                    }
                    var warmedUpVariantsCount = graphicsStateCollection.completedWarmupCount;
                    var warmupJob = graphicsStateCollection.WarmUpProgressively(_prewarmPerFrameCount);
                    await UniTask.NextFrame();
                    warmupJob.Complete();
                    
                    if (graphicsStateCollection.completedWarmupCount != lastWarmedUpVariantCount) {
                        collectionBatchesWarmupTimes.Add(Time.realtimeSinceStartup - lastWarmupStartTime);
                        lastWarmedUpVariantCount = graphicsStateCollection.completedWarmupCount;
                        lastWarmupStartTime = Time.realtimeSinceStartup;
                    }
                    
                    ReportProgress(completedWarmups + graphicsStateCollection.completedWarmupCount);
                    if (warmedUpVariantsCount == graphicsStateCollection.completedWarmupCount) {
                        // Sometimes WarmUpProgressively lefts some items unprocessed, so we call WarmUp to process them
                        warmupJob = graphicsStateCollection.WarmUp();
                        await UniTask.NextFrame();
                        warmupJob.Complete();
                        break;
                    }
                }

                completedWarmups += graphicsStateCollection.variantCount;
                ReportProgress(completedWarmups);

                await Resources.UnloadUnusedAssets();
                GCCleanup();
            }

            await UniTask.NextFrame();
            await Resources.UnloadUnusedAssets();
            GCCleanup();
            await UniTask.NextFrame();
            ReportProgress(1);
            ShadersPreloader.MarkPreloaded();
            await UniTask.NextFrame();

            Discard();
        }

        void GCCleanup() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        void ReportProgress(int warmedUpCount) {
            this.Trigger(Events.ProgressChanged, GetProgress(warmedUpCount));
        }

        float GetProgress(int warmedUpCount) => warmedUpCount / (float)_allItemsToPrewarmCount;
        
        static void AppendWarmupTimesToStringBuilder(StringBuilder sb, List<float> collectionBatchesWarmupTimes) {
            for (int i = 0; i < collectionBatchesWarmupTimes.Count; i++) {
                sb.Append(collectionBatchesWarmupTimes[i].ToString(CultureInfo.InvariantCulture));
                sb.Append(',').Append(' ');
            }
            sb.Length--;
        }
    }
}
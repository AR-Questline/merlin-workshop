using System;
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
        const int PreloadVariantsPerFrameCount = 20;
        const string PreloadVariantsPerFrameCountConfigName = "preload_shader_variants_per_frame_count";

        public sealed override bool IsNotSaved => true;

        ShaderVariantCollection[] _shaderVariantCollections;
        GraphicsStateCollection[] _graphicsStateCollections;
        
        int _allItemsToPrewarmCount;
        int _prewarmPerFrameCount;

        public new static class Events {
            public static readonly Event<ShadersPreloadingPanel, float> ProgressChanged = new(nameof(ProgressChanged));
        }

        public ShadersPreloadingPanel() {
            _prewarmPerFrameCount = Configuration.GetInt(PreloadVariantsPerFrameCountConfigName, PreloadVariantsPerFrameCount);

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

            foreach (var variantCollection in _shaderVariantCollections) {
                while (!variantCollection.isWarmedUp) {
                    var warmedUpVariantsCount = variantCollection.warmedUpVariantCount;
                    variantCollection.WarmUpProgressively(_prewarmPerFrameCount);
                    await UniTask.NextFrame();
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
                while (!graphicsStateCollection.isWarmedUp) {
                    var warmedUpVariantsCount = graphicsStateCollection.completedWarmupCount;
                    var warmupJob = graphicsStateCollection.WarmUpProgressively(_prewarmPerFrameCount);
                    await UniTask.NextFrame();
                    warmupJob.Complete();
                    ReportProgress(completedWarmups + graphicsStateCollection.completedWarmupCount);
                    if (warmedUpVariantsCount == graphicsStateCollection.completedWarmupCount) {
                        // Sometimes WarmUpProgressively lefts some items unprocessed, so we call WarmUp to process them
                        graphicsStateCollection.WarmUp();
                        await UniTask.NextFrame();
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
            var progress = warmedUpCount / (float)_allItemsToPrewarmCount;
            this.Trigger(Events.ProgressChanged, progress);
        }
    }
}
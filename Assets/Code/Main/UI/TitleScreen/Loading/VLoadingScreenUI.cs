using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Debugging;
using Awaken.TG.Graphics.Animations;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.UI.TitleScreen.Loading {
    [UsesPrefab("TitleScreen/VLoadingScreenUI")]
    public class VLoadingScreenUI : View<LoadingScreenUI> {
        const float UnloadPercent = 0.3f;
        const int MaxFramesForSoundBanksLoading = 20;
        public TextMeshProUGUI hintMessage;
        public Image loadingBarImage;

        [Space]
        [SerializeField] AssetLoadingGate assetLoadingGate;
        [SerializeField, FoldoutGroup("Backgrounds")] Image background;
        [SerializeField, FoldoutGroup("Backgrounds")] Image blurBackground;
        [SerializeField, FoldoutGroup("Backgrounds")] List<LoadingImageAsset> backgrounds;
        [SerializeField, FoldoutGroup("Backgrounds")] List<LoadingImageAsset> sarrasBackgrounds;

        Sequence _sequence;
        bool _sequenceAccepted;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            AsyncOnInitialize().Forget();
        }

        protected async UniTaskVoid AsyncOnInitialize() {
#if UNITY_EDITOR
            // When entering a scene directly random seems to return the same value most of the time
            Random.InitState(Environment.TickCount);
#endif
            if (Target.UseFastTransition == false) {
                var backgroundsToChoose = IsSarrasRegion() 
                    ? sarrasBackgrounds 
                    : backgrounds;
                
                int index = Random.Range(0, backgroundsToChoose.Count);
                var backgroundAsset = backgroundsToChoose[index];
                
                backgroundAsset.Background.RegisterAndSetup(this, background);
                backgroundAsset.BlurredBackground.RegisterAndSetup(this, blurBackground);
            }

            await FadeIn();
            Target.InitHeavy();
            StartCoroutine(LoadSceneAsync());
            return;

            bool IsSarrasRegion() {
                var commonReferences = CommonReferences.Get;
                if (commonReferences == null) {
                    return false;
                }
                
                if (Target.LoadingOperation.SceneToLoad == commonReferences.SarrasFirstSceneReference) {
                    return true;
                }
                
                var sceneCache = ScenesCache.Get;
                if (sceneCache == null) {
                    return false;
                }
                
                var sceneRegionReference = sceneCache.GetOpenWorldRegion(Target.LoadingOperation.SceneToLoad);
                return sceneRegionReference == commonReferences.SarrasCampaignSceneReference;
            }
        }

        async UniTask FadeIn() {
            await UniTask.WaitUntil(() => !World.HasAny<SavingWorldMarker>());
            await AsyncUtil.WaitWhile(gameObject, () => assetLoadingGate.gate.alpha == 0, 5f);
            if (Target.UseFastTransition == false || TitleScreen.wasLoadingFailed != LoadingFailed.False) {
                await UniTask.DelayFrame(1);
                await World.Services.Get<TransitionService>().ToCamera(LoadingScreenUI.ToCameraDuration);
            }

            await UniTask.WaitUntil(World.HasAny<SettingsMaster>);
        }

        IEnumerator LoadSceneAsync() {
            Tween hintTween = null;
            if (Target.UseFastTransition == false) {
                hintTween = DOTween.Sequence().SetUpdate(true)
                    .AppendCallback(() => hintMessage.text = RandomUtil.UniformSelect(LoadingHints.Hints, s => s != hintMessage.text))
                    .AppendInterval(15f)
                    .SetLoops(-1, LoopType.Incremental)
                    .Play();
            }

            Target.DisableLeshy();
            
            yield return Target.BeforeDroppingPreviousDomains();
            Target.Trigger(LoadingScreenUI.Events.BeforeDroppedPreviousDomain, Target);
            Target.DropPreviousDomains();
            Target.Trigger(LoadingScreenUI.Events.AfterDroppedPreviousDomain, Target);
            List<ISceneLoadOperation> unloadOperations = Target.UnloadPrevious().WhereNotNull().ToList();
            var unloadedScenesNames = new HashSet<string>(4);
            foreach (var unloadOperation in unloadOperations) {
                unloadedScenesNames.AddRange(unloadOperation.MainScenesNames);
            }

            var unloadedOperations = new HashSet<ISceneLoadOperation>();
            // wait for previous scenes to unload
            if (Target.UseFastTransition) {
                while (unloadOperations.Any(l => l.IsDone == false)) {
                    for (int i = 0; i < unloadOperations.Count; i++) {
                        if (unloadOperations[i].IsDone && unloadedOperations.Add(unloadOperations[i])) {
                            Log.Marking?.Warning($"Unloaded: {unloadOperations[i].Name}");
                        }
                    }

                    yield return null;
                }
            } else {
                while (unloadOperations.Any(l => l.IsDone == false)) {
                    float progress = unloadOperations.Sum(l => l.Progress) * UnloadPercent / unloadOperations.Count;
                    loadingBarImage.fillAmount = progress;
                    for (int i = 0; i < unloadOperations.Count; i++) {
                        if (unloadOperations[i].IsDone && unloadedOperations.Add(unloadOperations[i])) {
                            Log.Marking?.Warning($"Unloaded {unloadOperations[i].Name}");
                        }
                    }

                    yield return null;
                }
            }

            Log.Marking?.Warning("Unloaded all previous scenes");

            unloadOperations.Clear();
            unloadedOperations.Clear();

            // Mute audio
            Target.AddElement(new AudioMuter());
            yield return new WaitForEndOfFrame();

            // Wait for all templates loaded
            var templatesProvider = Services.Get<TemplatesProvider>();
            if (!templatesProvider.AllLoaded) {
                Log.Marking?.Warning("Waiting for templates to load");
                while (!templatesProvider.AllLoaded) {
                    yield return null;
                }
                Log.Marking?.Warning("All templates loaded");
            }

            // Unload Hero Body Prefab to free memory
            var vHeroController = Hero.Current?.View<VHeroController>();
            if (vHeroController != null) {
                vHeroController.BlockHeroBodyLoading();
                vHeroController.TryReloadBodyWithEquips().Forget();
            }
            
            // Wait for batch of assets unloading
            yield return null;
            yield return null;

            DOTween.Clear();
            AnimatorUtils.ResetCache();
            if (Target.ShouldUnloadUnused) {
                GC.Collect();
                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
            }

            // Wait for batch of assets unloading
            yield return null;
            yield return null;

            // Ensure unloaded assets
            if (Target.ShouldUnloadUnused) {
                GC.Collect();
                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
            }

            // Wait for batch of assets unloading
            yield return null;
            yield return new WaitForEndOfFrame();

            // Load Hero Body back
            if (vHeroController != null) {
                vHeroController.UnlockHeroBodyLoading();
            }
            
            // wait for scene to load
            ISceneLoadOperation loadOperation = Target.Load();
            if (Target.UseFastTransition) {
                while (loadOperation is { IsDone: false }) {
                    yield return null;
                }
            } else {
                while (loadOperation is { IsDone: false }) {
                    loadingBarImage.fillAmount = UnloadPercent + loadOperation.Progress * (1f - UnloadPercent);
                    yield return null;
                }
            }
            if (loadOperation == null) {
                Log.Marking?.Warning($"Loading: Exited Interior");
            } else {
                Log.Marking?.Warning($"Loaded: {loadOperation.Name}");
            }

            hintTween.Kill();
        }

        [Serializable]
        struct LoadingImageAsset {
            [SerializeField, UIAssetReference] SpriteReference background;
            [SerializeField, UIAssetReference] SpriteReference backgroundBlurQuality;

            public SpriteReference Background => background;
            public SpriteReference BlurredBackground => backgroundBlurQuality;
        }

    }
}
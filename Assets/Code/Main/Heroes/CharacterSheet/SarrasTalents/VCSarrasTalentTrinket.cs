using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI;
using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents {
    public class VCSarrasTalentTrinket : ViewComponent {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] CanvasGroup bgGoldCanvasGroup;
        [SerializeField] CanvasGroup eyeCanvasGroup;
        [SerializeField] CanvasGroup breathContentCanvasGroup;
        [SerializeField] CanvasGroup bgGoldBlur;
        [SerializeField] Image fillImage;
        [SerializeField] Component focusAfterCharge;
        [SerializeField] VCSyncImageFillWithMaterial fillSyncer;
        [SerializeField] float buttonHoldDuration = 0.8f;
        [SerializeField] float startGlowDuration = 0.2f;
        [SerializeField] float fadeOutDuration = 1.8f;
        [SerializeField] float scaleMin = 0.6f;
        [SerializeField] float scaleMax = 1.4f;
        [SerializeField] float breathDimValue = 0.25f;
        [SerializeField] float breathDuration = 1f;
        [SerializeField] float distributeFillDuration = 3f;
        [SerializeField] float waitForDistribution = 4f;
        [SerializeField] float distributionInterval = 1.25f;
        [SerializeField] VGenericPromptUI holdPromptUI;

        Tween _breathTween;
        Sequence _pointsSequence;
        float _holdStartTime;
        bool _heldButton;
        SarrasHeroTreeBranches _treeBranches;
        bool _catalystPointsAvailable;
        Prompt _holdPrompt;
        ARFmodEventEmitter _audioEmitter;

        ARFmodEventEmitter AudioEmitter {
            get {
                if (_audioEmitter == null) {
                    return _audioEmitter = CommonReferences.Get.PromptAudioEmitter;
                }

                return _audioEmitter;
            }
        }
        
        static CharacterStatType CatalystPointsStat => CharacterStatType.CatalystTalentPoints;
        static CharacterStatType MagePointsStat => CharacterStatType.SarrasMageTalentPoints;
        static CharacterStatType RoguePointsStat => CharacterStatType.SarrasRogueTalentPoints;
        static CharacterStatType WarriorPointsStat => CharacterStatType.SarrasWarriorTalentPoints;
        
        public static class Events {
            public static readonly Event<IModel, bool> PointsDistributionStarted = new(nameof(PointsDistributionStarted));
            public static readonly Event<IModel, bool> PointsDistributionInProgress = new(nameof(PointsDistributionInProgress));
            public static readonly Event<IModel, bool> PointsDistributionCompleted = new(nameof(PointsDistributionCompleted));
        }
        
        protected override void OnAttach() {
            _treeBranches = World.Only<SarrasHeroTreeBranches>();
            bgGoldCanvasGroup.alpha = 0f;
            
            InitButton();
            InitEvents();
            InitPrompts();
            ResetFillAmount();
            OnCatalystPointsChanged(true);
            OnFirstChargeCommitted(_treeBranches.IsFirstCharged);
        }

        void InitButton() {
            buttonConfig.InitializeButton();
            buttonConfig.button.OnEvent += OnTrinketButtonEvent;
            buttonConfig.button.OnPress += OnTrinketButtonPressed;
            buttonConfig.button.OnRelease += OnTrinketButtonReleased;
        }

        void InitEvents() {
            World.EventSystem.ListenTo(EventSelector.AnySource, TalentTreeUI.Events.TreeZoomedIn, this, OnTreeZoomedIn);

            if (!_treeBranches.IsFirstCharged) {
                World.EventSystem.ListenTo(EventSelector.AnySource, SarrasHeroTreeBranches.Events.FirstChargeCommitted, this, OnFirstChargeCommitted);
            }
            
            Hero.Current.ListenTo(Stat.Events.StatChanged(CatalystPointsStat), () => OnCatalystPointsChanged(false), this);
        }
        
        void InitPrompts() {
            var sarrasTalentOverview = World.Only<SarrasTalentOverviewUI>();
            _holdPrompt = sarrasTalentOverview.CharacterSheetUI.Prompts.BindPrompt(Prompt.VisualOnlyHold(KeyBindings.UI.Items.SelectItem, string.Empty), sarrasTalentOverview, holdPromptUI, visible: false);
        }
        
        void ResetFillAmount() {
            fillImage.fillAmount = 0f;
            fillSyncer.UnregisterUpdate();
        }

        void OnCatalystPointsChanged(bool initialize) {
            _catalystPointsAvailable = GetHeroStat(CatalystPointsStat).ModifiedInt > 0;
            bool trinketActive = _catalystPointsAvailable && TalentTree.IsUpgradeAvailable;
            buttonConfig.button.Interactable = trinketActive;
            _holdPrompt.SetVisible(trinketActive);
            if (initialize) {
                bgGoldBlur.TrySetActiveOptimized(false);
                eyeCanvasGroup.alpha = trinketActive ? 1f : 0f;
                buttonConfig.TrySetActiveOptimized(trinketActive);
                if (trinketActive) {
                    Breath();
                } else {
                    World.Only<Focus>().Select(focusAfterCharge);
                }
            }
        }

        void DistributePoints() {
            TriggerPointsDistributionStarted();
            buttonConfig.button.Interactable = false;
            World.Only<Focus>().Select(focusAfterCharge);
            
            fillSyncer.RegisterUpdate();
            _pointsSequence.Kill();
            _pointsSequence = DOTween.Sequence().SetUpdate(true)
                .Append(bgGoldCanvasGroup.DOFade(1f, startGlowDuration))
                .JoinCallback(() => bgGoldBlur.TrySetActiveOptimized(true))
                .Join(bgGoldBlur.transform.DOScale(scaleMax, startGlowDuration).SetEase(Ease.OutCubic))
                .JoinCallback(() => buttonConfig.TrySetActiveOptimized(false))
                .AppendInterval(waitForDistribution)
                .Join(bgGoldBlur.transform.DOScale(scaleMin, waitForDistribution).SetEase(Ease.InSine))
                .Append(bgGoldBlur.DOFade(0f, 0f))
                .AppendCallback(TriggerPointsDistributionInProgress)
                .Append(DOVirtual.Float(1f, 0f, distributeFillDuration, x => fillImage.fillAmount = x))
                .Join(bgGoldCanvasGroup.DOFade(0f, fadeOutDuration))
                .Join(eyeCanvasGroup.DOFade(0f, fadeOutDuration))
                .AppendInterval(distributionInterval)
                .AppendCallback(TriggerPointsDistributionCompleted)
                .OnComplete(fillSyncer.UnregisterUpdate);
        }

        void TriggerPointsDistributionStarted() {
            GenericTarget.Trigger(Events.PointsDistributionStarted, true);
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.SarrasSkillTreeTrinketActiveSound);
        }

        void TriggerPointsDistributionInProgress() {
            GenericTarget.Trigger(Events.PointsDistributionInProgress, true);
        }

        void TriggerPointsDistributionCompleted() {
            GenericTarget.Trigger(Events.PointsDistributionCompleted, true);
        }

        void Breath() {
            _breathTween?.Kill();
            _breathTween = breathContentCanvasGroup.DOCanvasFade(breathDimValue, breathDuration).SetUpdate(true).SetLoops(-1, LoopType.Yoyo);
        }

        void StopBreathTween() {
            _breathTween?.Kill();
            breathContentCanvasGroup.alpha = 1f;
        }

        void OnFirstChargeCommitted(bool state) {
            buttonConfig.button.navigation = state ? new Navigation { mode = Navigation.Mode.Explicit } : new Navigation { mode = Navigation.Mode.None };
        }

        void OnTreeZoomedIn(bool zoomIn) {
            gameObject.SetActiveOptimized(!zoomIn);
            if (zoomIn && (_pointsSequence?.IsPlaying() ?? false)) {
                _pointsSequence.Kill(true);
                bgGoldCanvasGroup.alpha = 0f;
                ResetFillAmount();
                OnCatalystPointsChanged(true);
            }
        }

        void ConvertPoints() {
            Stat catalystPoints = GetHeroStat(CatalystPointsStat);
            if (catalystPoints <= 0) {
                return;
            }
            
            Stat magePoints = GetHeroStat(MagePointsStat);
            Stat roguePoints = GetHeroStat(RoguePointsStat);
            Stat warriorPoints = GetHeroStat(WarriorPointsStat);

            magePoints.IncreaseBy(catalystPoints.ModifiedInt);
            roguePoints.IncreaseBy(catalystPoints.ModifiedInt);
            warriorPoints.IncreaseBy(catalystPoints.ModifiedInt);
            
            catalystPoints.DecreaseBy(catalystPoints.ModifiedInt);
            
            if (!_treeBranches.IsFirstCharged && TalentTree.IsUpgradeAvailable) {
                _treeBranches.CommitFirstCharge();
            }
        }

        static Stat GetHeroStat(StatType statType) {
            return Hero.Current.Stat(statType);
        }
        
        void OnTrinketButtonReleased() {
            if (RewiredHelper.IsGamepad) {
                return;
            }

            if (_heldButton) {
                float holdTime = Time.unscaledTime - _holdStartTime;
                if (holdTime < buttonHoldDuration) {
                    // AudioEmitter.Stop();
                    ResetFillAmount();
                    if (_catalystPointsAvailable) {
                        Breath();
                    }
                }
                _heldButton = false;
            }
        }

        void OnTrinketButtonPressed() {
            if (_catalystPointsAvailable) {
                // AudioEmitter.PlayNewEventWithPauseTracking(CommonReferences.Get.AudioConfig.SarrasSkillTreeTrinketChargeSound);
            }
            
            if (!_heldButton) {
                _heldButton = true;
                _holdStartTime = Time.unscaledTime;
                StopBreathTween();
            }
        }

        UIResult OnTrinketButtonEvent(UIEvent action) {
            if (!_catalystPointsAvailable) {
                return UIResult.Ignore;
            }
            
            if (action is UIKeyHeldAction or UIEMouseHeld) {
                if (_heldButton) {
                    fillSyncer.RegisterUpdate();
                    float holdTime = Time.unscaledTime - _holdStartTime;
                    if (holdTime <= buttonHoldDuration) {
                        fillImage.fillAmount = holdTime / buttonHoldDuration;
                    } else {
                        _heldButton = false;
                        fillImage.fillAmount = 1f;
                        ConvertPoints();
                        DistributePoints();
                    }
                    return UIResult.Accept;
                }
            } else if (action is UIKeyUpAction or UIEMouseUp) {
                if (_heldButton) {
                    float holdTime = Time.unscaledTime - _holdStartTime;
                    if (holdTime < buttonHoldDuration) {
                        ResetFillAmount();
                    } else {
                        OnTrinketButtonReleased();
                    }
                    _heldButton = false;
                    return UIResult.Accept;
                }
            }
            
            return UIResult.Ignore;
        }

        protected override void OnDiscard() {
            _breathTween.Kill();
            _breathTween = null;
            _pointsSequence.Kill();
            _pointsSequence = null;
        }
    }
}
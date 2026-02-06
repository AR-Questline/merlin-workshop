using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.TreeUI;
using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.SarrasTalents {
    public class VCSarrasTreeBranchButton : ViewComponent {
        const float DimValue = 0.33f;
        
        [SerializeField] ARButton branchButton;
        [SerializeField] TalentTreeBranchType branchType;
        [SerializeField] Image selectedIcon;
        [SerializeField] CanvasGroup mainBgCanvasGroups;
        [SerializeField] CanvasGroup slotsCanvasGroups;
        [SerializeField] Transform glowTransform;
        [SerializeField] float glowDuration = 1f;
        [SerializeField] float glowScale = 2f;
        [SerializeField] float waitForGlow = 0.2f;

        Sequence _glowSequence;
        SarrasHeroTreeBranches _treeBranches;
        
        public static class Events {
            public static readonly Event<IModel, TalentTreeBranchType> TalentTreeBranchClicked = new(nameof(TalentTreeBranchClicked));
        }

        protected override void OnAttach() {
            branchButton.OnHover += OnHover;
            branchButton.OnClick += OnClicked;
            _treeBranches = World.Only<SarrasHeroTreeBranches>();

            if (_treeBranches.CurrentlySelected == TalentTreeBranchType.None) {
                World.EventSystem.LimitedListenTo(EventSelector.AnySource, Talent.Events.TalentChanged, this, OnTalentChanged, 1);
            }
            
            World.EventSystem.ListenTo(EventSelector.AnySource, TalentTreeUI.Events.TreeZoomedIn, this, OnTreeZoomed);
            World.EventSystem.ListenTo(EventSelector.AnySource, VCSarrasTalentTrinket.Events.PointsDistributionInProgress, this, OnPointsDistributionInProgress);
            
            if (!_treeBranches.IsFirstCharged) {
                DimBg(true);
                _treeBranches.ListenToLimited(SarrasHeroTreeBranches.Events.FirstChargeCommitted, state => {
                    DimBg(false);
                    SetInteractable(state);
                }, this);
            }
            
            _treeBranches.ListenTo(SarrasHeroTreeBranches.Events.TalentTreeBranchChanged, type => BranchChanged(type), this);
            BranchChanged(_treeBranches.CurrentlySelected, true);
            SetInteractable(_treeBranches.IsFirstCharged);
        }

        void OnTalentChanged(Talent talent) {
            if (talent.TalentTreeBranchType.ToSarrasTreeBranchType() == branchType) {
                _treeBranches.SelectTalentTreeBranch(branchType);
            }
        }

        void OnTreeZoomed(bool zoom) {
            if (zoom) {
                DimSlots(false);
            } else {
                DimSlots(_treeBranches.CurrentlySelected != branchType);
            }
        }

        void OnClicked() {
            GenericTarget.Trigger(Events.TalentTreeBranchClicked, branchType);
        }

        void BranchChanged(TalentTreeBranchType type, bool initialize = false) {
            bool isSelected = type == branchType;
            DimSlots(!isSelected);
            selectedIcon.TrySetActiveOptimized(isSelected);
            glowTransform.TrySetActiveOptimized(isSelected);
            if (isSelected && !initialize) {
                _glowSequence.Kill(true);
                var localScale = glowTransform.localScale;
                _glowSequence = DOTween.Sequence().SetUpdate(true)
                    .Append(glowTransform.DOScale(localScale * glowScale, glowDuration).SetEase(Ease.OutSine))
                    .Append(glowTransform.DOScale(localScale, glowDuration).SetEase(Ease.InSine))
                    .AppendCallback(() => glowTransform.TrySetActiveOptimized(false));
                
                FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.SarrasSkillTreeBranchSelectedSound);
            }
        }
        
        void OnPointsDistributionInProgress() {
            _glowSequence.Kill();
            glowTransform.TrySetActiveOptimized(true);
            var localScale = glowTransform.localScale;
            _glowSequence = DOTween.Sequence().SetUpdate(true)
                .AppendInterval(waitForGlow)
                .Append(glowTransform.DOScale(localScale * glowScale, glowDuration).SetEase(Ease.OutSine))
                .Append(glowTransform.DOScale(localScale, glowDuration).SetEase(Ease.InSine))
                .AppendCallback(() => glowTransform.TrySetActiveOptimized(false));
        }

        void OnHover(bool hover) {
            if (hover) {
                DelayedHover().Forget();
            } else {
                GenericTarget?.Trigger(SarrasTalentOverviewUI.Events.TalentTreeBranchHovered, TalentTreeBranchType.None);
            }
        }

        // hack for order issues with OnHover in ARButton
        async UniTaskVoid DelayedHover() {
            if (await AsyncUtil.DelayFrame(GenericTarget, 2)) {
                GenericTarget.Trigger(SarrasTalentOverviewUI.Events.TalentTreeBranchHovered, branchType);
            }
        }
        
        void DimSlots(bool dim) {
            slotsCanvasGroups.alpha = dim ? DimValue : 1;
        }
        
        void DimBg(bool dim) {
            mainBgCanvasGroups.alpha = dim ? DimValue : 1;
        }
        
        void SetInteractable(bool interactable) {
            slotsCanvasGroups.interactable = !interactable;
            slotsCanvasGroups.blocksRaycasts = interactable;
            mainBgCanvasGroups.interactable = !interactable;
            mainBgCanvasGroups.blocksRaycasts = interactable;
        }

        protected override void OnDiscard() {
            _glowSequence.Kill();
            _glowSequence = null;
        }
    }
}
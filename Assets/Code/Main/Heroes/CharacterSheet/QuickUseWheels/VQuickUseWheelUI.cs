using System;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Heroes.Resting;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Gems.GemManagement;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.RadialMenu;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    [UsesPrefab("CharacterSheet/QuickUseWheel/VQuickUseWheelUI")]
    public class VQuickUseWheelUI : VRadialMenuUI<QuickUseWheelUI> {
        [Space(10f)]
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float appearTweenDuration = 0.1f;
        [SerializeField] float disappearTweenDuration = 0.1f;
        [SerializeField] TimeScaleTransition appearTimeScaleTransition;
        [SerializeField] QuickUseDescription description;
        [SerializeField] VCQuickUsePages quickUsePages;
        [SerializeField] VCQuickItemTooltipUI quickItemTooltipUIPrimary;
        [SerializeField] VCQuickItemTooltipUI quickItemTooltipUISecondary;
        
        Sequence _appearSequence;
        Sequence _changePageSequence;
        DirectTimeMultiplier _timeMultiplier;
        static bool IsHeroInCombat => Hero.Current.IsInCombat();

        public ref QuickUseDescription Description => ref description;
        public VCQuickItemTooltipUI QuickItemTooltipUIPrimary => quickItemTooltipUIPrimary;
        public VCQuickItemTooltipUI QuickItemTooltipUISecondary => quickItemTooltipUISecondary;

        protected override void OnInitialize() {
            _timeMultiplier = new DirectTimeMultiplier(1, ID);
            World.Only<GlobalTime>().AddTimeModifier(_timeMultiplier);
            base.OnInitialize();
        }
        
        public void HideItemTooltips() {
            quickItemTooltipUIPrimary.HideItem();
            quickItemTooltipUISecondary.HideItem();
        }

        protected override void InitPrompts() {
            bool canPlayerSave = LoadSave.Get.CanPlayerSave();
            
            if (CheatController.CheatsEnabled()) {
                Prompt restPrompt = Prompt.Tap(KeyBindings.UI.QuickWheel.QuickWheelRest, LocTerms.Rest.Translate(), OpenRestUI);
                Prompts.AddPrompt<VBrightPromptUI>(restPrompt, Target, canPlayerSave, canPlayerSave);
            }

            var quickSavePrompt = Prompt.Tap(KeyBindings.UI.QuickWheel.QuickWheelQuickSave, LocTerms.QuickSave.Translate(), TryQuickSave);
            Prompts.AddPrompt<VBrightPromptUI>(quickSavePrompt, Target, canPlayerSave, canPlayerSave);
            
            base.InitPrompts();
        }

        static void TryQuickSave() {
            if (LoadSave.Get.CanPlayerSave()) {
                LoadSave.Get.QuickSave().Forget();
            } else {
                SaveLoadUnavailableInfo.ShowSaveUnavailableInfo();
            }
        }

        [UnityEngine.Scripting.Preserve]
        void NextPage() {
            if (!_fullyAppear || (_changePageSequence?.IsPlaying() ?? false)) {
                return;
            }
            
            Description.HideAll();
            _changePageSequence?.Kill(true);
            
            ClearOptions();
            _changePageSequence = DOTween.Sequence().SetUpdate(true)
                .Append(quickUsePages.NextPage())
                .OnComplete(() => {
                    SetupAfter(SetupDelayFrame).Forget();
                });
        }

        void OpenRestUI() {
            if (IsHeroInCombat) {
                return;
            }
            
            World.Add(new RestPopupUI(withTransition: true));
            Close();
        }

        protected override IBackgroundTask OnDiscard() {
            _timeMultiplier.Remove();
            return base.OnDiscard();
        }

        protected override void Appear() {
            Description.HideAll();
            canvasGroup.alpha = appearTimeScaleTransition.from;
            _appearSequence = DOTween.Sequence().SetUpdate(true).SetEase(Ease.OutQuad)
                .Append(DOTween.To(() => canvasGroup.alpha, alpha => canvasGroup.alpha = alpha, 1, appearTweenDuration))
                .Join(DOTween.To(() => Time.timeScale, _timeMultiplier.Set, appearTimeScaleTransition.to, appearTimeScaleTransition.duration));
        }

        protected override void Disappear() {
            if (Target is null or { HasBeenDiscarded:true }) {
                return;
            }
            
            _appearSequence.Kill();
            DOTween.Sequence().SetUpdate(true).SetEase(Ease.InQuad)
                .Append(DOTween.To(() => canvasGroup.alpha, alpha => canvasGroup.alpha = alpha, 0, disappearTweenDuration))
                .Join(DOTween.To(() => Time.timeScale, _timeMultiplier.Set, 1, disappearTweenDuration))
                .AppendCallback(Target.Discard);
        }

        protected override VCRadialMenuOption<QuickUseWheelUI> InitialOptionFrom(VCRadialMenuOption<QuickUseWheelUI>[] options) {
            int currentLoadoutIndex = Hero.Current.HeroItems.CurrentLoadoutIndex;
            return options.FirstOrAny(option => option is VCQuickLoadout loadout && loadout.LoadoutIndex == currentLoadoutIndex);
        }

        [Serializable]
        struct TimeScaleTransition {
            public float duration;
            public float from;
            public float to;
        }
    }
}
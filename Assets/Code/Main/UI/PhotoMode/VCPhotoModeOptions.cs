using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Settings.GammaSettingScreen;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.UI.PhotoMode {
    public class VCPhotoModeOptions : ViewComponent<PhotoModeUI> {
        [SerializeField] ButtonConfig poseFocusButtonConfig;
        [SerializeField] ButtonConfig filterFocusButtonConfig;
        [SerializeField] ButtonConfig brightnessFocusButtonConfig;
        [SerializeField] ButtonConfig contrastFocusButtonConfig;
        [SerializeField] ButtonConfig saturationFocusButtonConfig;
        [SerializeField] ARMultiOptions poseOptions;
        [SerializeField] ARMultiOptions filterOptions;
        [SerializeField] ARMultiOptionsSlider gammaSlider;
        [SerializeField] ARMultiOptionsSlider contrastSlider;
        [SerializeField] ARMultiOptionsSlider saturationSlider;

        [SerializeField] GameObject[] filterEffects = Array.Empty<GameObject>();

        int _currentPoseIndex;
        int _currentFilterIndex;

        GammaSetting _gammaSetting;
        float _gammaInitialValue;
        
        ContrastSetting _contrastSetting;
        float _contrastInitialValue;
        
        protected override void OnAttach() {
            InitButtons();
            InitPoses();
            InitFilters();
            InitGammaSlider();
            InitContrastSlider();
            InitSaturationSlider();
        }

        void InitButtons() {
            poseFocusButtonConfig.InitializeButton();
            poseFocusButtonConfig.button.OnEvent += poseOptions.Handle;
            filterFocusButtonConfig.InitializeButton();
            filterFocusButtonConfig.button.OnEvent += filterOptions.Handle;
            brightnessFocusButtonConfig.InitializeButton();
            brightnessFocusButtonConfig.button.OnEvent += gammaSlider.Handle;
            contrastFocusButtonConfig.InitializeButton();
            contrastFocusButtonConfig.button.OnEvent += contrastSlider.Handle;
            saturationFocusButtonConfig.InitializeButton();
            saturationFocusButtonConfig.button.OnEvent += saturationSlider.Handle;
        }

        void InitPoses() {
            poseOptions.Initialize(LocTerms.Pose.Translate(), () => {
                _currentPoseIndex = (_currentPoseIndex - 1 + Target.AnimationPosesCount) % Target.AnimationPosesCount;
                Target.PreviousPoseChanged(_currentPoseIndex);
                poseOptions.SetValueText(_currentPoseIndex.ToString());
            }, () => {
                _currentPoseIndex = (_currentPoseIndex + 1) % Target.AnimationPosesCount;
                Target.NextPoseChanged(_currentPoseIndex);
                poseOptions.SetValueText(_currentPoseIndex.ToString());
            }, _currentPoseIndex.ToString());
        }

        void InitFilters() {
            filterOptions.Initialize(LocTerms.Filter.Translate(), () => {
                _currentFilterIndex = (_currentFilterIndex - 1 + filterEffects.Length) % filterEffects.Length;
                SwitchFilterEffect(_currentFilterIndex);
                filterOptions.SetValueText(_currentFilterIndex.ToString());
            }, () => {
                _currentFilterIndex = (_currentFilterIndex + 1) % filterEffects.Length;
                SwitchFilterEffect(_currentFilterIndex);
                filterOptions.SetValueText(_currentFilterIndex.ToString());
            }, _currentFilterIndex.ToString());
            SwitchFilterEffect(_currentFilterIndex);
        }

        void InitGammaSlider() {
            _gammaSetting = World.Only<GammaSetting>();
            _gammaInitialValue = _gammaSetting.Value;
            gammaSlider.Initialize(LocTerms.SettingsGamma.Translate(), val => {
                Target.Trigger(PhotoModeUI.Events.GammaChanged, val);
            }, GammaScreen.MinValue, GammaScreen.MaxValue, GammaScreen.SliderStepChange, _gammaInitialValue);
        }

        void InitContrastSlider() {
            _contrastSetting = World.Only<ContrastSetting>();
            _contrastInitialValue = _contrastSetting.Value;
            contrastSlider.Initialize(LocTerms.SettingsContrast.Translate(), val => {
                Target.Trigger(PhotoModeUI.Events.ContrastChanged, val);
            }, ContrastSetting.MinValue, ContrastSetting.MaxValue, ContrastSetting.SliderStepChange, _contrastInitialValue);
        }

        void InitSaturationSlider() {
            saturationSlider.Initialize(LocTerms.Saturation.Translate(), val => {
                Target.Trigger(PhotoModeUI.Events.SaturationChanged, val);
            }, -100f, step: 10f);
        }
        
        void SwitchFilterEffect(int index) {
            for (int i = 0; i < filterEffects.Length; i++) {
                filterEffects[i].SetActiveOptimized(i == index);
            }
        }

        protected override void OnDiscard() {
            Target.Trigger(PhotoModeUI.Events.GammaChanged, _gammaInitialValue);
            Target.Trigger(PhotoModeUI.Events.ContrastChanged, _contrastInitialValue);
        }
    }
}
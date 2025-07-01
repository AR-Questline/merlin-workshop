using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Options.Views {
    [UsesPrefab("Settings/VDependentOption")]
    public class VDependentOption : View<AllSettingsUI>, IVSetting {
        
        public Transform parent, dependentParent;
        DependentOption _option;

        IVSetting _baseOptionView;
        List<IVSetting> _otherOptionsViews = new List<IVSetting>();
        Navigation _originalNavigation;

        public Selectable MainSelectable => _baseOptionView.MainSelectable;

        public void Setup(PrefOption option) {
            _option = (DependentOption) option;
            _baseOptionView = SettingsUtil.SpawnView(Target, _option.BaseOption, parent);
        }
        
        void Update() {
            if (_option.ShowDependent && !_otherOptionsViews.Any()) {
                _originalNavigation = MainSelectable.navigation;
                Selectable previous = MainSelectable;
                foreach (var otherOption in _option.OtherOptions) {
                    IVSetting view = SettingsUtil.SpawnView(Target, otherOption, dependentParent);
                    _otherOptionsViews.Add(view);
                    previous = SettingsUtil.EstablishNavigation(previous, view.MainSelectable);
                }
            } else if (!_option.ShowDependent && _otherOptionsViews.Any()) {
                _otherOptionsViews.ForEach(v => v.Discard());
                _otherOptionsViews.Clear();
                MainSelectable.navigation = _originalNavigation;
            }
        }

        protected override IBackgroundTask OnDiscard() {
            if (Target.HasBeenDiscarded) {
                return base.OnDiscard();
            }
            
            _baseOptionView.Discard();
            _otherOptionsViews.ForEach(v => v.Discard());
            return base.OnDiscard();
        }
    }
}
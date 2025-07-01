using System.Collections.Generic;
using Awaken.TG.Main.Settings.Options;
using Awaken.Utility.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Accessibility {
    public abstract partial class CanvasScaleSetting : Setting {
        public const bool WholeNumbers = true, Synchronize = true;
        public const string WholeNumberFormat = "{0:0}";
        public const float DefaultValue = 100f, StepChange = 1f;
        public const int BaseWidth = 1920, BaseHeight = 1080;

        protected virtual float MinScale => 80f;
        protected virtual float MaxScale => 110f;
        
        protected Vector2 CurrentScale => new(BaseWidth / (MainSliderOption.Value / 100), BaseHeight / (MainSliderOption.Value / 100f));
        
        protected SliderOption MainSliderOption { get; init; }
        public override IEnumerable<PrefOption> Options => MainSliderOption.Yield();

        protected abstract void UpdateSettings();

        protected override void OnApply() {
            UpdateSettings();
        }

        protected SliderOption CreateSliderOption(string id, string name) {
            return new SliderOption(id, name, MinScale, MaxScale, WholeNumbers, WholeNumberFormat, DefaultValue, Synchronize, StepChange);
        }

        protected void ResizeCanvasScaler(Canvas canvas) {
            if (canvas != null) {
                canvas.GetComponent<CanvasScaler>().referenceResolution = CurrentScale;
            }
        }
    }
}
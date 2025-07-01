using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.PhotoMode;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Graphics;
using QFSW.QC;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(Volume))]
    public class GammaController : StartDependentView<GammaSetting> {
        Volume _volume;
        LiftGammaGain _liftGamma;

        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            if (!_volume.isGlobal) {
                return;
            }
            _volume.TryGetVolumeComponent(out _liftGamma);
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, PhotoModeUI.Events.GammaChanged, this, SetBrightness);
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            GammaSetting gammaSetting = (GammaSetting) setting;
            SetBrightness(gammaSetting.Value);
        }

        void SetBrightness(float brightness) {
            // calculation are based on these graphs
            // https://www.desmos.com/calculator/5zb8usyeox
            brightness -= 1;

            float gamma = 0;
            float gain = 0;
            if (brightness > 0) {
                gamma = math.pow(brightness, 1.599462f);
            } else if (brightness < 0) {
                gain = brightness * 0.95f;
                gamma = (math.log(gain + 1) + -4.60517018f) / -4.60517018f - 1;
            }
            
            var gammaVector = _liftGamma.gamma.value;
            gammaVector.w = gamma;
            _liftGamma.gamma.value = gammaVector;
            _liftGamma.gamma.overrideState = true;
            
            var gainVector = _liftGamma.gain.value;
            gainVector.w = gain;
            _liftGamma.gain.value = gainVector;
            _liftGamma.gain.overrideState = true;
        }
        
        [Command("brightness.gamma", "")][UnityEngine.Scripting.Preserve]
        public static void PrintGamma() {
            var controller = FindAnyObjectByType<GammaController>();
            if (controller) {
                QuantumConsole.Instance.LogToConsoleAsync("Gamma: " + controller._liftGamma.gamma.value.w);
            }
        }
        
        [Command("brightness.gamma", "")][UnityEngine.Scripting.Preserve]
        public static void PrintGamma(float gamma) {
            var controller = FindAnyObjectByType<GammaController>();
            if (controller) {
                var value = controller._liftGamma.gamma.value;
                value.w = gamma;
                controller._liftGamma.gamma.value = value;
                controller._liftGamma.gamma.overrideState = true;
            }
            PrintGamma();
        }
        
        [Command("brightness.gain", "")][UnityEngine.Scripting.Preserve]
        public static void PrintGain() {
            var controller = FindAnyObjectByType<GammaController>();
            if (controller) {
                QuantumConsole.Instance.LogToConsoleAsync("Gain: " + controller._liftGamma.gain.value.w);
            }
        }
        
        [Command("brightness.gain", "")][UnityEngine.Scripting.Preserve]
        public static void PrintGain(float gain) {
            var controller = FindAnyObjectByType<GammaController>();
            if (controller) {
                var value = controller._liftGamma.gain.value;
                value.w = gain;
                controller._liftGamma.gain.value = value;
                controller._liftGamma.gain.overrideState = true;
            }
            PrintGain();
        }
    }
}
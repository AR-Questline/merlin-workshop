using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using System;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Main.Heroes.Interactions {
    [UsesPrefab("HUD/Interactions/" + nameof(VInteractionHoldSequence))]
    public class VInteractionHoldSequence : View<HeroInteractionHoldUI> {
        [SerializeField] GameObject holdContainer;
        [SerializeField] HoldSequenceData[] holdGraphicSequence = Array.Empty<HoldSequenceData>();

        protected override void OnInitialize() {
            holdContainer.SetActiveOptimized(Target.FancyHoldGraphic);
            
            foreach (var holdSequence in holdGraphicSequence) {
                holdSequence.Init();
            }
        }

        public void SetActive(bool active) {
            holdContainer.SetActiveOptimized(active);
        }
        
        public void SetHoldPercent(float percent) {
            foreach (var holdSequence in holdGraphicSequence) {
                holdSequence.Fill(percent);
            }
        }

        [Serializable]
        public struct HoldSequenceData {
            [SerializeField] Image graphic;
            [SerializeField, Range(0,1)] float startPercent;
            [SerializeField, Range(0,1)] float endPercent;
            [SerializeField] HoldAnimation animationType;
            [SerializeField, ShowIf(nameof(animationType), HoldAnimation.Color)] Color startColor;
            [SerializeField, ShowIf(nameof(animationType), HoldAnimation.Color)] Color endColor;

            public void Init() {
                switch (animationType) {
                    case HoldAnimation.Fill:
                        graphic.fillAmount = 0;
                        break;
                    case HoldAnimation.Color:
                        graphic.color = startColor;
                        break;
                    default: 
                        Log.Important?.Error($"HoldSequenceData: {animationType} not implemented");
                        break;
                }
            }
            
            public void Fill(float percent) {
                float value = (percent - startPercent) / (endPercent - startPercent);
                
                switch (animationType) {
                    case HoldAnimation.Fill:
                        graphic.fillAmount = value;
                        break;
                    case HoldAnimation.Color:
                        graphic.color = Color.Lerp(startColor, endColor, value);
                        break;
                    default: 
                        Log.Important?.Error($"HoldSequenceData: {animationType} not implemented");
                        break;
                }
            }
        }

        enum HoldAnimation : byte {
            Fill,
            Color
        }
    }
}
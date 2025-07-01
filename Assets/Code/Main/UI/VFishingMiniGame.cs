using System.Collections.Generic;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI {
    [UsesPrefab("HUD/VFishingMiniGame")]
    public class VFishingMiniGame : View<FishingMiniGame> {
        const float MinRodHpPadding = 0f;
        const float MaxRodHpPadding = 391f;
        const float HpToBubblesMultiplier = 10f;

        [SerializeField] RectTransform fishRange;
        [SerializeField] Image fishRangeImage;
        [SerializeField] RectTransform hook;
        [SerializeField] RectMask2D rodHpHidingMask;
        [SerializeField] List<Image> bubbles;
        [SerializeField] Canvas canvas;
        [SerializeField] KeyIcon keyIcon;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();

        protected override void OnInitialize() {
            Target.ListenTo(FishingMiniGame.Events.SetViewActive, SetActive, this);
            Target.ListenTo(FishingMiniGame.Events.OnMiniGameStart, OnMiniGameStart, this);
            Target.ListenTo(FishingMiniGame.Events.OnMiniGameTick, UpdateFishingMiniGameUI, this);
        }
        
        public void SetActive(bool isActive) {
            canvas.enabled = isActive;
        }

        public void OnMiniGameStart(FishingMiniGame fishingMiniGame) {
            ResetBubbles();
            SetActive(true);
            fishRange.localScale = new Vector3(fishRange.localScale.x, (Target.FishRange * 2f) / 100f, fishRange.localScale.z);
            keyIcon.Setup(new KeyIcon.Data(KeyBindings.Gameplay.Interact, true), this);
        }

        void UpdateFishingMiniGameUI() {
            hook.anchorMin = new Vector2(0.5f, Target.RodPosition / 100f);
            hook.anchorMax = new Vector2(0.5f, Target.RodPosition / 100f);
            fishRange.anchorMin = new Vector2(0.5f, Target.FishPosition / 100f);
            fishRange.anchorMax = new Vector2(0.5f, Target.FishPosition / 100f);
            fishRangeImage.color = Target.IsWithinRange() ? ARColor.MainAccent : ARColor.MainGrey;
            rodHpHidingMask.padding = new Vector4(0f, 0f, 0f, Mathf.Lerp(MaxRodHpPadding, MinRodHpPadding, Target.RodHealth / Target.MaxRodHealth));
            UpdateBubbles();
        }

        void UpdateBubbles() {
            for (int i = bubbles.Count - 1; i >= 0; i--) {
                float fishHealthPercentToBubblesValue = Target.FishHealth / Target.MaxFishHealth * HpToBubblesMultiplier;
                if (fishHealthPercentToBubblesValue < i) {
                    bubbles[i].color = ARColor.Transparent;
                } else {
                    var newColor = bubbles[i].color;
                    newColor.a = fishHealthPercentToBubblesValue - i;
                    bubbles[i].color = newColor;
                    break;
                }
            }
        }

        void ResetBubbles() {
            foreach (var bubble in bubbles) {
                bubble.color = ARColor.MainWhite;
            }
        }
    }
}
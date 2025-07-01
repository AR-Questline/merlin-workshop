using Awaken.TG.MVC.UI.Handlers.Tooltips;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI {
    public static class CardsStaticTooltipUtility {
        [UnityEngine.Scripting.Preserve]
        public static void UpdateStaticTooltipPositioning(StaticTooltipData data) {
            if (data.staticPositioning == null) return;
            var x = data.cardTransform.position.x;
            float xInputOnScreen = x / Screen.width;
            if (xInputOnScreen > 0.77f) {
                data.staticPositioning.position = data.leftTooltipPosition.position;
                data.staticPositioning.pivot = data.leftTooltipPosition.pivot;
            } else {
                data.staticPositioning.position = data.tooltipPosition.position;
                data.staticPositioning.pivot = data.tooltipPosition.pivot;
            }

            data.staticPositioning.allowOffset = false;
            data.subStaticPositioning.position = data.subTooltipPosition.position;
            data.subStaticPositioning.pivot = data.subTooltipPosition.pivot;
            data.subStaticPositioning.scale = data.subTooltipPosition.localScale.x;
            data.subStaticPositioning.allowOffset = false;
        }
    }

    public readonly struct StaticTooltipData {
        public readonly Transform cardTransform;
        public readonly StaticPositioning staticPositioning, subStaticPositioning;
        public readonly RectTransform tooltipPosition, leftTooltipPosition, subTooltipPosition;

        [UnityEngine.Scripting.Preserve]
        public StaticTooltipData(Transform cardTransform, StaticPositioning staticPositioning, StaticPositioning subStaticPositioning, RectTransform tooltipPosition, RectTransform leftTooltipPosition, RectTransform subTooltipPosition) {
            this.cardTransform = cardTransform;
            this.staticPositioning = staticPositioning;
            this.subStaticPositioning = subStaticPositioning;
            this.tooltipPosition = tooltipPosition;
            this.leftTooltipPosition = leftTooltipPosition;
            this.subTooltipPosition = subTooltipPosition;
        }
    }
}

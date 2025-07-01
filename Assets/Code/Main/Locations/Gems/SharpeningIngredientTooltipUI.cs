using System;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    public partial class SharpeningIngredientTooltipUI : ItemTooltipUI {
        public SharpeningIngredientTooltipUI(Type viewType, Transform host, float appearDelay = -1, float hideDelay = -1,
            float alphaTweenTime = 0.25f, bool isStatic = false, bool preventDisappearing = false,
            bool comparerActive = true) : base(viewType, host, appearDelay, hideDelay, alphaTweenTime, isStatic,
            preventDisappearing, comparerActive) { }
    }
}
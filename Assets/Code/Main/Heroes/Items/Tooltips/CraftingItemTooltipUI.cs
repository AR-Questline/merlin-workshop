using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public partial class CraftingItemTooltipUI : ItemTooltipUI {
        public new static class Events {
            public static readonly Event<IModel, bool> ResultTooltipDisplayed = new(nameof(ResultTooltipDisplayed));
        }
        
        public CraftingItemTooltipUI(Type viewType, Transform host, float appearDelay = -1, float hideDelay = -1, float alphaTweenTime = 0.25f, bool isStatic = false, bool comparerActive = true, bool preventDisappearing = false) : 
            base(viewType, host, appearDelay, hideDelay, alphaTweenTime, isStatic, comparerActive, preventDisappearing) { }

        public void AfterCreated() {
            this.Trigger(Events.ResultTooltipDisplayed, true);
        }
        
        public void DisappearTooltip() {
            this.Trigger(Events.ResultTooltipDisplayed, false);
        }
    }
}
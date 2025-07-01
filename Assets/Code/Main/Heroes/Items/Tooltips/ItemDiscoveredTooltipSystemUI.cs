using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    public partial class ItemDiscoveredTooltipSystemUI : CraftingItemTooltipUI {
        VItemDiscoveredTooltipSystemUI View => View<VItemDiscoveredTooltipSystemUI>();
        
        public ItemDiscoveredTooltipSystemUI(Type viewType, Transform host, float appearDelay = -1, float hideDelay = -1, float alphaTweenTime = 0.25f, bool isStatic = false, bool comparerActive = true, bool preventDisappearing = false) 
            : base(viewType, host, appearDelay, hideDelay, alphaTweenTime, isStatic, comparerActive, preventDisappearing) { }
        
        public Tween ShowToolTip(float duration) {
            return View.ShowTooltip(duration);
        }
    }
}
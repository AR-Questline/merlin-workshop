using System;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Base {
    public partial class FloatingTooltipUI : Element {
        public sealed override bool IsNotSaved => true;

        readonly Transform _host;
        readonly bool _isStatic;
        readonly bool _comparerActive;
        readonly Type _viewType;
        
        public float AppearDelay { get; }
        public float HideDelay { get; }
        public float AlphaTweenTime { get; }
        public bool PreventDisappearing { get; }
        
        public FloatingTooltipUI(Type viewType, Transform host, float appearDelay = -1f, float hideDelay = -1, float alphaTweenTime = 0.25f, bool isStatic = false, bool preventDisappearing = false) {
            _viewType = viewType;
            _host = host;
            _isStatic = isStatic;
            AppearDelay = appearDelay;
            HideDelay = hideDelay;
            AlphaTweenTime = alphaTweenTime;
            PreventDisappearing = preventDisappearing;
        }
        
        protected override void OnInitialize() {
            World.SpawnView(this, _viewType, true, true, _host);
        }
        
        public void SetPosition(TooltipPosition left, TooltipPosition right) {
            if (_isStatic || View<VFloatingTooltipSystemUI>() == null) {
                return;
            }
            
            View<VFloatingTooltipSystemUI>().SetPosition(left, right);
        }
        
        public void ForceDisappear() {
            View<VFloatingTooltipSystemUI>().ForceDisappear();
        }
    }
}

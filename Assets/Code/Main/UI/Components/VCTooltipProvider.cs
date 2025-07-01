using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class VCTooltipProvider : MonoBehaviour, IWithTechnicalTooltip {

        public bool showTooltip = true;
        [LocStringCategory(Category.UI)]
        public LocString tooltipText;
        [LocStringCategory(Category.UI)]
        public LocString technicalText;
        public RectTransform tooltipHost;
        
        public UIResult Handle(UIEvent evt) => UIResult.Ignore;

        TooltipConstructor GetTooltipConstructor(string text) {
            TooltipConstructor constructor = text;
            if (constructor != null && tooltipHost != null) {
                constructor.StaticPositioning = StaticPositioning;
            }
            return constructor;
        }
        
        TooltipConstructor _tooltipConstructor;
        public TooltipConstructor TooltipConstructor {
            get {
                if (_tooltipConstructor == null || !_tooltipConstructor.StaticPositioning.Equals(StaticPositioning)) {
                    _tooltipConstructor = GetTooltipConstructor(tooltipText);
                }

                return _tooltipConstructor;
            }
        }

        TooltipConstructor _technicalTooltipConstructor;
        public TooltipConstructor TechnicalTooltipConstructor {
            get {
                if(showTooltip)
                    return _technicalTooltipConstructor ??= string.IsNullOrWhiteSpace(technicalText) ? TooltipConstructor : technicalText;
                return null;
            }
        }
        
        StaticPositioning StaticPositioning =>
            _staticPositioning ??= new StaticPositioning {
                position = tooltipHost.position,
                pivot = tooltipHost.pivot,
                allowOffset = true,
                scale = tooltipHost.localScale.x,
                positionChangeAllowedSqr = 15,
                pivotChangeAllowedSqr = 0,
            };

        StaticPositioning _staticPositioning;

        public void Assign(TooltipConstructor constructor) {
            _tooltipConstructor = constructor;
        }

        public void Assign(string text) => Assign((LocString) text);
        public void Assign(LocString text) {
            tooltipText = text;
            _tooltipConstructor = null;
        }

        void Update() {
            if (tooltipHost == null) {
                return;
            }
            if (StaticPositioning.position != (Vector2) tooltipHost.position) {
                _staticPositioning.position = tooltipHost.position;
            }
        }
    }
}
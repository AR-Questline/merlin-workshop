using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    public abstract class VCTooltipElement : ViewComponent<ITooltip>{
        [RichEnumExtends(typeof(TooltipElement))]
        public RichEnumReference elementReference;
        public TooltipElement TooltipElement => elementReference.EnumAs<TooltipElement>();
        public abstract void UpdateContent(object value);
    }
}
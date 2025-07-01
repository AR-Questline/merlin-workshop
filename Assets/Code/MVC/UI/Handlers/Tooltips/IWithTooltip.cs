
namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    /// <summary>
    /// Interface for models that display tooltips on hover.
    /// </summary>
    public interface IWithTooltip : IUIAware {
        TooltipConstructor TooltipConstructor { get; }
    }
}
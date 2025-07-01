
namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    /// <summary>
    /// Interface for models that display tooltips on hover AND technical tooltip on RMB
    /// </summary>
    public interface IWithTechnicalTooltip : IWithTooltip {
        TooltipConstructor TechnicalTooltipConstructor { get; }
    }
}
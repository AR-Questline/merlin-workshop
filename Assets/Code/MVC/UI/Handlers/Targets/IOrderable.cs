namespace Awaken.TG.MVC.UI.Handlers.Targets
{
    /// <summary>
    /// Interface for stuff that you can order around by selecting it and right-clicking.
    /// </summary>
    public interface IOrderable : IModel
    {        
        /// <summary>
        /// Accepts an order from the map UI. Returns true if the order resulted
        /// in executing something, false if it was rejected.
        /// </summary>
        bool AcceptTarget(TargetChange target);

        /// <summary>
        /// Accepts hovering of potential target.
        /// </summary>
        bool AcceptTargetHover(TargetChange target);
    }
}

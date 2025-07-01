using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    public interface ITooltip : IModel {
        TooltipConstructor Constructor { get; }
        bool MoveWithMouse { get; }
        Vector2 TargetPosition { get; }
        Vector2 TargetPivot { get; }
        float Scale { get; }
        [UnityEngine.Scripting.Preserve] bool Visible { get; }
        ITooltip Parent { get; }
    }
}
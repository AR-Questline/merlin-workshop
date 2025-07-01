using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.Selections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    public interface IItemUI : IElement {
        [UnityEngine.Scripting.Preserve] Item Item { get; }
        [UnityEngine.Scripting.Preserve] Transform Host { get; }
        
        [UnityEngine.Scripting.Preserve] bool IsSelected => Item != null && World.Only<Selection>().IsSelected(Item);
        [UnityEngine.Scripting.Preserve] bool CanBeBought => true;
        [UnityEngine.Scripting.Preserve] string LeftText => "";
        [UnityEngine.Scripting.Preserve] string RightText => "";
        
        [UnityEngine.Scripting.Preserve] void OnSubmit();
    }
}
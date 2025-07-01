using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu {
    [UnityEngine.Scripting.Preserve]
    public class FocusBaseTab : ITab {
        Transform Parent { get; }

        public FocusBaseTab(Transform cardsTransform) {
            Parent = cardsTransform;
        }

        public bool IsActive => World.Only<Focus>().FocusBase == Parent;
        public void Select() {
            World.Only<Focus>().SwitchToFocusBase(Parent);
        }
    }
}
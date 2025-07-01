using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components.Navigation {
    public class ExplicitNavi : MonoBehaviour, INaviOverride {
        public Selectable up;
        public Selectable down;
        public Selectable left;
        public Selectable right;

        public UIResult resultIfNull;

        public UIResult Navigate(UINaviAction navi) {
            NaviDirection direction = navi.direction;
            if (direction == NaviDirection.Up) {
                return Select(up, navi);
            } else if (direction == NaviDirection.Down) {
                return Select(down, navi);
            } else if (direction == NaviDirection.Left) {
                return Select(left, navi);
            } else if (direction == NaviDirection.Right) {
                return Select(right, navi);
            }
            return resultIfNull;
        }

        UIResult Select(Selectable selectable, UINaviAction navi) {
            if (selectable?.gameObject.activeSelf ?? false) {
                World.Only<Focus>().Select(selectable);
                return UIResult.Accept;
            }
            return selectable?.GetComponentInChildren<INaviOverride>()?.Navigate(navi) ?? resultIfNull;
        }
    }
}
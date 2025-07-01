using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Focuses {
    /// <summary>
    /// Component we force to be focused when we want to unfocus anything on the screen.
    /// You can choose what direction NaviAction must have to deselect it
    /// </summary>
    public class VCEmptyFocus : ViewComponent, IUIAware {
        public bool naviUp;
        public bool naviDown;
        public bool naviLeft;
        public bool naviRight;

        Focus Focus => World.Only<Focus>();
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UINaviAction navi) {
                bool up = naviUp && navi.direction == NaviDirection.Up;
                bool down = naviDown && navi.direction == NaviDirection.Down;
                bool left = naviLeft && navi.direction == NaviDirection.Left;
                bool right = naviRight && navi.direction == NaviDirection.Right;

                if (up || down || left || right) {
                    Deselect();
                }
                return UIResult.Accept;
            }

            if (evt is UIEPointTo) {
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }

        public void Select() {
            Focus.SwitchToFocusBase(transform);
            Focus.Select(this);
        }

        public void Deselect() {
            Focus.Deselect(this);
            Focus.RemoveFocusBase(transform);
        }
    }
}
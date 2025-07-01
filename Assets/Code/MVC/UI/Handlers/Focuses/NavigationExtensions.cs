using System;
using UnityEngine.UI;

namespace Awaken.TG.MVC.UI.Handlers.Focuses {
    public static class NavigationExtensions {
        public static void ChangeNavi(this Selectable selectable, Func<Navigation, Navigation> changeFunc) {
            Navigation navi = selectable.navigation;
            navi = changeFunc(navi);
            selectable.navigation = navi;
        }
        
        public static bool IsVerticalMovement(this NaviDirection direction) {
            return direction == NaviDirection.Up || direction == NaviDirection.Down;
        }
    }
}
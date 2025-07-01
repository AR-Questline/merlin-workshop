using UnityEngine;

namespace Awaken.TG.MVC.UI.Universal {
    /// <summary>
    /// When attached to GameObject that contains Selectable component, that Selectable will be excluded from automatic navigation. 
    /// </summary>
    public class NaviBlocker : MonoBehaviour, INaviBlocker {
        public bool AllowNavigation => false;
    }
}
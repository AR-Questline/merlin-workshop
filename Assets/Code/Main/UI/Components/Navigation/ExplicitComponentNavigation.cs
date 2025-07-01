using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components.Navigation {
    [Serializable]
    public struct ExplicitComponentNavigation {
        [SerializeField] GameObject up;
        [SerializeField] GameObject down;
        [SerializeField] GameObject left;
        [SerializeField] GameObject right;

        [UnityEngine.Scripting.Preserve] public GameObject Up => up;
        [UnityEngine.Scripting.Preserve] public GameObject Down => down;
        [UnityEngine.Scripting.Preserve] public GameObject Left => left;
        [UnityEngine.Scripting.Preserve] public GameObject Right => right;
        
        static void Select(GameObject go) {
            if (go != null) {
                World.Only<Focus>().Select((Component)go.GetComponent<IUIAware>());
            }
        }
        
        public bool TryHandle(UIEvent evt, out UIResult result) {
            result = UIResult.Accept;
            if (evt is UINaviAction navi) {
                var target = GetNavigationTarget(navi.direction);
                if (target != null) {
                    Select(target);
                }
                return true;
            }
            return false;
        }

        public GameObject GetNavigationTarget(NaviDirection direction) {
            if (direction == NaviDirection.Up) {
                return up;
            }
            if (direction == NaviDirection.Down) {
                return down;
            }
            if (direction == NaviDirection.Left) {
                return left;
            } 
            if (direction == NaviDirection.Right) {
                return right;
            }

            return null;
        }
    }
}
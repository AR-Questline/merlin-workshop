using System;
using Awaken.TG.Main.Utility.UI;
using Awaken.Utility.Enums;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.MVC.UI.Handlers.Focuses {
    public class NaviDirection : RichEnum{
        Func<Selectable, Selectable> _getter;
        Action<Navigation, Selectable> _setter;
        public float LastTick { get; private set; }
        public float AxisValue { get; private set; }

        int _continuousUsages = 0;
        float NaviDeltaTime => _continuousUsages > 1 ? 0.15f : 0.3f;

        public static readonly NaviDirection
            Down = new NaviDirection(nameof(Down), s => s.FindSelectableOnDown(), (a, b) => a.selectOnDown = b),
            Up = new NaviDirection(nameof(Up), s => s.FindSelectableOnUp(), (a, b) => a.selectOnUp = b),
            Right = new NaviDirection(nameof(Right), s => s.FindSelectableOnRight(), (a, b) => a.selectOnRight = b),
            Left = new NaviDirection(nameof(Left), s => s.FindSelectableOnLeft(), (a, b) => a.selectOnLeft = b);

        protected NaviDirection(string enumName, Func<Selectable, Selectable> getter, Action<Navigation, Selectable> setter, string inspectorCategory = "")
            : base(enumName, inspectorCategory) {
            _getter = getter;
            _setter = setter;
        }

        public Selectable GetFrom(Selectable selectable) {
            Selectable result = _getter(selectable);
            if (result == null && selectable.navigation.mode == Navigation.Mode.Explicit) {
                selectable.ChangeNavi(n => {
                    n.mode = Navigation.Mode.Automatic;
                    return n;
                });
                result = _getter(selectable);
                selectable.ChangeNavi(n => {
                    n.mode = Navigation.Mode.Explicit;
                    return n;
                });
            }
            return result;
        }

        [UnityEngine.Scripting.Preserve]
        public void SetTo(Selectable selectable, Selectable target) {
            Navigation navi = selectable.navigation;
            _setter(navi, target);
            selectable.navigation = navi;
        }

        public bool Use() {
            bool canUse = Time.realtimeSinceStartup - LastTick > NaviDeltaTime;
            if (canUse) {
                LastTick = Time.realtimeSinceStartup;
                _continuousUsages++;
            }
            return canUse;
        }

        void Update(float axis) {
            AxisValue = axis;
            if (axis <= RewiredHelper.ContinuousNaviThreshold) {
                _continuousUsages = 0;
            }
        }

        public static void Update(Player player) {
            float horizontal = 0;//player.GetAxisRaw("Horizontal");
            float vertical = 0;//player.GetAxisRaw("Vertical");
            
            Up.Update(vertical);
            Down.Update(-vertical);
            Right.Update(horizontal);
            Left.Update(-horizontal);
        }
    }
}
using System;
using Awaken.TG.MVC.UI.Universal;

namespace Awaken.TG.MVC.Attributes {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class SpawnsView : Attribute, IComparable<SpawnsView> {
        public Type view;
        public bool isMainView;
        public string forceParentMember;
        public int order;

        public SpawnsView(Type view, bool isMainView = true, string forceParentMember = "", int order = 0) {
            this.view = view;
            this.isMainView = isMainView;
            this.forceParentMember = forceParentMember;
            this.order = typeof(VModalBlocker).IsAssignableFrom(view) ? -100 : order;
        }

        public int CompareTo(SpawnsView other) {
            return order - other.order;
        }
    }
}

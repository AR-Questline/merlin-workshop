using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.RadialMenu {
    public abstract class VCRadialMenuOption<T> : ViewComponent<T> where T : IRadialMenuUI {
        public bool isQuickAction;
        public VRadialMenuUI<T> RadialMenu => (VRadialMenuUI<T>) ParentView;

        public float Theta { get; private set; }
        
        Vector3 _initialPosition;

        void Awake() {
            _initialPosition = transform.localPosition;
        }

        public virtual void ResetOption() {
            transform.localPosition = _initialPosition;
        }
        
        public void Setup(Vector3 offset) {
            Setup(offset.x, offset.y);
        }
        
        void Setup(float x, float y) {
            Theta = Mathf.Atan2(y, x);
        }

        public abstract void OnHoverStart();
        public abstract void OnHoverEnd();
        public abstract void OnSelect(bool onClose);

        public virtual OptionDescription Description => OptionDescription.Empty;


        public struct OptionDescription {
            public bool active;
            public string name;

            public OptionDescription(bool active, string name) {
                this.active = active;
                this.name = name;
            }

            public static OptionDescription Empty => new(false, LocTerms.UIGenericUse.Translate());
        }
    }
}
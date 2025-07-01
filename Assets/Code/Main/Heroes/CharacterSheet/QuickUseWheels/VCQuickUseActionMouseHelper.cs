using Awaken.TG.Main.Utility.Semaphores;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public class VCQuickUseActionMouseHelper : ViewComponent, IUIAware, ISemaphoreObserver {
        [SerializeField] VCQuickUseAction quickAction;

        CoyoteSemaphore _isHovered;

        void Start() {
            _isHovered = new CoyoteSemaphore(this);
        }

        void Update() {
            _isHovered.Update();
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UIEPointTo) {
                _isHovered.Notify();
                return UIResult.Accept;
            }

            if (evt is UIEMouseDown { IsLeft: true }) {
                quickAction.OnSelect(false);
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }

        void ISemaphoreObserver.OnUp() => quickAction.OnHoverStart();
        void ISemaphoreObserver.OnDown() => quickAction.OnHoverEnd();
    }
}
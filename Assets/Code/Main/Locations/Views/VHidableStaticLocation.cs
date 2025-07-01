using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.Locations.Views {
    [UsesPrefab("Locations/VLocationStatic")]
    public class VHidableStaticLocation : VStaticLocation, IVLocationWithState {
        bool _isVisible = true;
        
        // === Initialization
        protected override void OnInitialize() {
            base.OnInitialize();
            InitVisibility();
            Target.ListenTo(Model.Events.AfterChanged, UpdateState, this);
        }

        // === Visibility
        void InitVisibility() {
            if (!Target.VisibleToPlayer) {
                Hide();
            } else {
                Show();
            }
        }
        
        public void UpdateState() {
            bool shouldBeVisible = Target.VisibleToPlayer && Target.IsCulled != true;
            if (!shouldBeVisible && _isVisible) {
                Hide();
            } else if (shouldBeVisible && !_isVisible) {
                Show();
            }
        }
        
        void Show() {
            _isVisible = true;
            Target.ViewParent.gameObject.SetActive(true);
            Target.Trigger(Location.Events.LocationVisibilityChanged, true);
        }

        void Hide() {
            _isVisible = false;
            Target.Trigger(Location.Events.LocationVisibilityChanged, false);
            Target.ViewParent.gameObject.SetActive(false);
        }
    }
}
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    [SpawnsView(typeof(VLockpickingAnimations))]
    public partial class LockpickingAnimations : Element<LockpickingInteraction> {
        public Transform AnimationsParent => ParentModel.AnimationsParent;

        new VLockpickingAnimations MainView => View<VLockpickingAnimations>();

        public bool IsBlocked => MainView.IsBlocked;

        public void PlayStartAnimation() {
            MainView.PlayStartAnimation();
        }

        public void PlayPicklockBrokenAnimation() {
            MainView.PlayPicklockBrokenAnimation();
        }

        public void PlayNextLevelAnimation() {
            MainView.PlayNextLevelAnimation();
        }

        public void PlayLockOpenedAnimation() {
            MainView.PlayLockOpenedAnimation();
        }

        public void PlayPicklockDamageAnimation() {
            MainView.PlayPicklockDamageAnimation();
        }
        public void PlayNoPicklockAnimation() {
            MainView.PlayNoPicklockAnimation();
        }
    }
}

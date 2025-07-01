using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    public abstract class VAdvancedNotification<TNotification> : View<TNotification>, IAdvancedNotificationsView where TNotification : IAdvancedNotification {
        [SerializeField] EventReference notificationSound;
        public EventReference NotificationSound => notificationSound;

        public override Transform DetermineHost() => Target.GenericParentModel.View<IViewNotificationBuffer>().NotificationParent;

        protected override void OnFullyInitialized() {
            PlaySound().Forget();
        }

        async UniTask PlaySound() {
            if(await AsyncUtil.WaitUntil(gameObject, () => Time.timeScale > 0)) {
                if (!notificationSound.IsNull) {
                    FMODManager.PlayOneShot(notificationSound);
                }
            }
        }

        //set this as animator event
        public virtual void DiscardNotification() {
            Target.Discard();
        }

    }
}
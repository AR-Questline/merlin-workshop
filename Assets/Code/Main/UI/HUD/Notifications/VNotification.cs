using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.Notifications {
    [UsesPrefab("HUD/Notifications/VNotification")]
    public class VNotification : View<Notification> {
        static readonly int AnimatorKey = Animator.StringToHash("IsVisible");

        public override Transform DetermineHost() => World.Only<NotificationBuffer>().View<VNotificationBuffer>().notificationParent;

        // === Inspector Properties & References
        public TextMeshProUGUI titleText;

        Animator _animator;

        protected override void OnInitialize() {
            titleText.text = Target.title.ToUpper();
            _animator = GetComponent<Animator>();
            Target.ParentModel.View<VNotificationBuffer>().WhenVisible(Show);
        }

        // === Animation
        void Show() {
            _animator.SetBool(AnimatorKey, true);
        }

        public void SetAnimatorSpeed(float speed) {
            _animator.speed = speed;
        }
    }
}
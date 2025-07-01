using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem.Notifications {
    public class NotificationsAudioService : MonoBehaviour, IService {
        [SerializeField] ARFmodEventEmitter notificationEventEmitter;
        
        NotificationSoundEvent _currentNotificationSoundEvent;
        
        public void PlayNotificationSound(NotificationSoundEvent notificationSoundEvent) {
            // if (!notificationEventEmitter.IsPlaying() || notificationSoundEvent.priority >= _currentNotificationSoundEvent.priority) {
            //     _currentNotificationSoundEvent = notificationSoundEvent;
            //     notificationEventEmitter.PlayNewEventWithPauseTracking(notificationSoundEvent.eventReference);
            // }
        }
    }
}
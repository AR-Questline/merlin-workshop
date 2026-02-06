using Awaken.TG.Main.Locations;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.TG.Main.Utility.Animations;
using UnityEngine;
using UnityEngine.Playables;

namespace Awaken.TG.Main.TimeLines {
    public class LocationAnimatorPropertyReceiver : MonoBehaviour, INotificationReceiver {
        public void OnNotify(Playable origin, INotification notification, object context) {
            if (notification is LocationAnimatorSetPropertyMarker animatorProperty) {
                foreach (Location location in animatorProperty.locationReference.MatchingLocations(null)) {
                    location.TryGetElement<AnimatorElement>()?.SetParameter(
                        animatorProperty.parameterValue.type,
                        Animator.StringToHash(animatorProperty.parameterName),
                        animatorProperty.parameterValue);
                }
            }
        }
    }
}
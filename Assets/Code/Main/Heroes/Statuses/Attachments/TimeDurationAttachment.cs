using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Statuses.Attachments {
    public class TimeDurationAttachment : MonoBehaviour, IDurationAttachment {
        public float time;
        public bool unscaledTime = true;
        
        public Element SpawnElement() {
            return new TimeDuration(time, unscaledTime);
        }

        public bool IsMine(Element element) {
            return element is TimeDuration;
        }
    }
}
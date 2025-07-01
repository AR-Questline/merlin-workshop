using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Manages elevator state and movement.")]
    public class ElevatorPlatformAttachment : MonoBehaviour, IAttachmentSpec {
        public Transform platformTransform;
        public float speed = 4;
        public bool useCustomDownwardsSpeed;
        [ShowIf(nameof(useCustomDownwardsSpeed))] public float customDownwardsSpeed = 4;
        public GameObject navmeshAddObject;
        public ARFmodEventEmitter elevatorEmitter;
        public ARFmodEventEmitter cogsEmitter;
        
        public Element SpawnElement() => new ElevatorPlatform();

        public bool IsMine(Element element) => element is ElevatorPlatform;

        void Reset() {
            platformTransform = transform;
        }
    }
}
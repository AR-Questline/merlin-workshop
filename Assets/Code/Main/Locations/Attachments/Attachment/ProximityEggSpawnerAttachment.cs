using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    public class ProximityEggSpawnerAttachment : MonoBehaviour, IAttachmentSpec {
        [Title("On Proximity")]
        [Range(0,1)] public float chanceToTriggerOnBandChange = 0.7f;
        [Range(0,1)] public float chanceToSpawnOnBandChange = 0.6f;
        [Title("On Egg Death")]
        [Range(0,1)] public float chanceToSpawnOnDeath = 0.2f;
        [Range(0,1)] public float chanceToSpawnedOnesToBeKilledOnDeath = 0.7f;
        
        public Element SpawnElement() => new ProximityEggSpawner();
        public bool IsMine(Element element) => element is ProximityEggSpawner;
    }
}
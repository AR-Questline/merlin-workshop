using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners.Arena {
    public class ArenaSpawnerAttachment : MonoBehaviour, IAttachmentSpec {
        public GameObject templatesSpawnPoint;
        public GameObject spawnersParent;
        public int maxUnitsInRow = 6;
        public bool showSpawnAmountPopup;
        public bool setHealthToMaxInt;
        
        public Element SpawnElement() {
            return new ArenaSpawner();
        }

        public bool IsMine(Element element) => element is ArenaSpawner;
    }
}
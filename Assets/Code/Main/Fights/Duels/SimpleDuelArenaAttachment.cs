using System;
using System.IO;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Duels {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical,
        "Marks Duel Arena for Duels (crime or story based), it's used to teleport NPCs and Hero into positions.")]
    public class SimpleDuelArenaAttachment : MonoBehaviour, IAttachmentSpec {
        [BoxGroup("Positions"), SerializeField] SimpleDuelArena.PositionType positionType = SimpleDuelArena.PositionType.RandomOnCircle;
        [BoxGroup("Positions"), SerializeField, ShowIf(nameof(ShowRandomOnCircle))] float arenaRadius = 10;
        [BoxGroup("Positions"), SerializeField, ShowIf(nameof(ShowRandomOnCircle))] float duelistsSpawnRadius = 2;
        [BoxGroup("Positions"), SerializeField, ShowIf(nameof(ShowSpecificPositions))] SimpleDuelArena.GroupPosition[] specificPositions = Array.Empty<SimpleDuelArena.GroupPosition>();
        [SerializeField] GameObject objectToActivate;

        public SimpleDuelArena.PositionType PositionType => positionType;
        public float ArenaRadius => arenaRadius;
        public float DuelistsSpawnRadius => duelistsSpawnRadius;
        public SimpleDuelArena.GroupPosition[] SpecificPositions(int groupSize) {
            if (specificPositions.Length == 0) {
                throw new InvalidDataException($"Simple Duel Arena is not set up correctly. Specified positions are empty. {LogUtils.GetDebugName(this)}");;
            }
            if (specificPositions.Length < groupSize) {
                throw new InvalidDataException($"Simple Duel Arena is not set up correctly. Specified positions lenght is smaller than requested length {groupSize}. {LogUtils.GetDebugName(this)}");
            }
            if (specificPositions.Length == groupSize) {
                return specificPositions;
            }
            var groupPositions = new SimpleDuelArena.GroupPosition[groupSize];
            for (int i = 0; i < groupSize; i++) {
                groupPositions[i] = specificPositions[i];
            }
            return groupPositions;
        }

        public GameObject ObjectToActivate => objectToActivate;

        
        bool ShowRandomOnCircle => positionType == SimpleDuelArena.PositionType.RandomOnCircle;
        bool ShowSpecificPositions => positionType == SimpleDuelArena.PositionType.SpecifiedPositions;
        
        public Element SpawnElement() {
            return new SimpleDuelArena();
        }

        public bool IsMine(Element element) => element is SimpleDuelArena;
        
#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            switch (positionType) {
                case SimpleDuelArena.PositionType.RandomOnCircle:
                    Gizmos.DrawWireSphere(transform.position, arenaRadius);
                    break;
                case SimpleDuelArena.PositionType.SpecifiedPositions: {
                    foreach (var groupPosition in specificPositions) {
                        Gizmos.DrawWireSphere(transform.position + groupPosition.positionOffset, groupPosition.spawnRadius);
                    }
                    break;
                }
            }
        }
#endif
    }
}

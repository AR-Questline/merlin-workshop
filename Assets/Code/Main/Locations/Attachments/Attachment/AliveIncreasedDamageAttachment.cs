using System;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.SerializableTypeReference;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    public class AliveIncreasedDamageAttachment : MonoBehaviour, IAttachmentSpec {
        public float damageMultiplier = 1.5f;
        
        public bool filterByNpcType = true;
        [ShowIf(nameof(filterByNpcType))]
        public NpcType[] npcTypesFilter = new[] {
            NpcType.MiniBoss,
            NpcType.Boss
        };
        
        public bool applyToSpecificProjectiles = false;

        [ShowIf(nameof(applyToSpecificProjectiles)), TypeDrawerSettings(BaseType = typeof(ProjectileBehaviour))]
        public SerializableTypeReference[] specificProjectileTypes = Array.Empty<SerializableTypeReference>();
        
        public Element SpawnElement() => new AliveIncreasedDamage();
        public bool IsMine(Element element) => element is AliveIncreasedDamage;
    }
}
using Awaken.TG.Main.AI.Combat.CustomDeath;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    [RequireComponent(typeof(NpcPresenceAttachment))]
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Kills the NPC on spawn.")]
    public class NpcKillOnSpawnAttachment : MonoBehaviour, IAttachmentSpec {
        public bool disableCorpseAlert = true;
        public bool useCustomAnimation;
        [ShowIf(nameof(useCustomAnimation))] public CustomDeathAnimation customDeathAnimation;
        
        public Element SpawnElement() => new NpcKillOnSpawnElement();
        public bool IsMine(Element element) => element is NpcKillOnSpawnElement;
    }
}
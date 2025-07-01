using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For items that can add an NPC fight behaviour.")]
    public class ItemAddNpcBehaviourSpec : MonoBehaviour, IAttachmentSpec {
        [field: SerializeReference] EnemyBehaviourBase _behaviour;

        public EnemyBehaviourBase Behaviour => _behaviour;

        public Element SpawnElement() {
            return new ItemAddNpcBehaviour();
        }

        public bool IsMine(Element element) {
            return element is ItemAddNpcBehaviour;
        }
    }
}
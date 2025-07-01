using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Buffs {
    [RequireComponent(typeof(ItemProjectileAttachment))]
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that can be thrown.")]
    public class ItemThrowableAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] float throwForce = 10f;
        
        public float ThrowForce => throwForce;
        
        public Element SpawnElement() {
            return new ItemThrowable();
        }

        public bool IsMine(Element element) => element is ItemThrowable;
    }
}
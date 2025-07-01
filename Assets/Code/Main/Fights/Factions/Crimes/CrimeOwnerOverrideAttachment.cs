using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public class CrimeOwnerOverrideAttachment : MonoBehaviour, IAttachmentSpec {
        [InfoBox("Empty will override default owner to none")]
        [SerializeField, TemplateType(typeof(CrimeOwnerTemplate)), HideLabel]
        TemplateReference ownerOverride;
        
        public CrimeOwnerTemplate OwnerOverride => ownerOverride.TryGet<CrimeOwnerTemplate>();
        public Element SpawnElement() => new CrimeOwnerOverride();
        public bool IsMine(Element element) => element is CrimeOwnerOverride;
    }
}
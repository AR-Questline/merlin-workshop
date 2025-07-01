using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public class ManualItemChargesAttachment : MonoBehaviour, IAttachmentSpec {
        [field: SerializeField] public int ChargesToSpend { get; private set; } = 1;
        public Element SpawnElement() => new ManualItemCharges();

        public bool IsMine(Element element) => element is ManualItemCharges;
    }
}
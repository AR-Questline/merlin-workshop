using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    [DisallowMultipleComponent]
    public class UnidentifiedItemAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] int costOfIdentification;
        [InfoBox("Only one item at a time can be identified.")]
        [SerializeField] LootTableWrapper lootTable;
        
        public int CostOfIdentification => costOfIdentification;
        public LootTableWrapper LootTableWrapper => lootTable;
        
        public Element SpawnElement() => new UnidentifiedItem();

        public bool IsMine(Element element) => element is UnidentifiedItem;
    }
}
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC.Elements;
using FMODUnity;
using System;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [RequireComponent(typeof(AliveLocationAttachment))]
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Wood chopping location.")]
    public class LumberingAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] GameObject[] backupObjects = Array.Empty<GameObject>();
        public LootTableWrapper lootTable;
        public Transform activeObject;
        public EventReference hitSound;

        public GameObject[] BackupObjects => backupObjects;
        
        public Element SpawnElement() {
            return new LumberingAction();
        }
        
        public bool IsMine(Element element) => element is LumberingAction;
    }
}
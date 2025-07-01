using System;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Clearing {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Sets flag based on conditions.")]
    public class SetFlagOnConditionAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, Tags(TagsCategory.Flag)]
        string flagToSet;
        [SerializeReference] Condition[] conditions = Array.Empty<Condition>();
        
        public Element SpawnElement() => new SetFlagOnCondition();
        public bool IsMine(Element element) => element is SetFlagOnCondition;

        public string FlagToSet => flagToSet;
        public Condition[] Conditions => conditions;
    }
}
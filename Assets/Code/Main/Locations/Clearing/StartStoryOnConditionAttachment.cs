using System;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Clearing {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Starts story based on conditions.")]
    public class StartStoryOnConditionAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] StoryBookmark story;
        [SerializeReference] Condition[] conditions = Array.Empty<Condition>();
        
        public Element SpawnElement() => new StartStoryOnCondition();
        public bool IsMine(Element element) => element is StartStoryOnCondition;

        public StoryBookmark Story => story;
        public Condition[] Conditions => conditions;
    }
}
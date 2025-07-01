using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track owned items.")]
    public class ItemTrackerAttachment : BaseSimpleTrackerAttachment {
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference[] items = Array.Empty<TemplateReference>();
        public IEnumerable<ItemTemplate> ItemTemplates => items.Select(i => i.Get<ItemTemplate>());
        
        public override Element SpawnElement() {
            return new ItemTracker();
        }

        public override bool IsMine(Element element) => element is ItemTracker;

        protected override string DisplayPatternDescription => base.DisplayPatternDescription +
                                                               "\n{item} - required item name";
    }
}
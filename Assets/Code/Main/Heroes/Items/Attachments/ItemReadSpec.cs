using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Common, "For items that can be read, contains story graph.")]
    public class ItemReadSpec : MonoBehaviour, IAttachmentSpec {
        
        [SerializeField, TemplateType(typeof(StoryGraph))]
        TemplateReference itemReadable;

        public TemplateReference StoryRef => itemReadable;

        public Element SpawnElement() {
            return new ItemRead();
        }

        public bool IsMine(Element element) {
            return element is ItemRead;
        }
        
#if UNITY_EDITOR
        public void EDITOR_SetStoryGraph(StoryGraph graph) {
            itemReadable = new TemplateReference(graph);
        }
#endif
    }
}
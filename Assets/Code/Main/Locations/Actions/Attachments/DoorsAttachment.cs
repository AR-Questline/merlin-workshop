using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Manages doors (animations, audio, auto closing, etc.).")]
    public class DoorsAttachment : MonoBehaviour, IAttachmentSpec {
        [TemplateType(typeof(StoryGraph))] 
        public TemplateReference storyOnInteract;
        public bool openByDefault;
        public EventReference openSound, closeSound;
        [EnumToggleButtons] public CloseAtTime closeAtTime;

        [FoldoutGroup(ILogicReceiverElement.GroupName)]
        public bool useReceiverEvenIfDisabled;
        
        public Element SpawnElement() {
            return new DoorsAction(openByDefault);
        }

        public bool IsMine(Element element) {
            return element is DoorsAction;
        }
    }
    
    public enum CloseAtTime : byte {
        [UnityEngine.Scripting.Preserve] None = 0,
        CloseAtNightBegin = 1,
        CloseAtNightEnd = 2,
        CloseAtNightChange = 3,
    }
}
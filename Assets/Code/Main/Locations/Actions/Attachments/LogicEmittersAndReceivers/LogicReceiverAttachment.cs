using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Receives logic events from Logic Emitter (f.e. doors opened by lever).")]
    public class LogicReceiverAttachment : MonoBehaviour, IAttachmentSpec {
        [InfoBox("Will transfer events from Logic Emitter to Animator or Animator Attachment.\nUses: \n - FailedActive animator Trigger\n - Active animator Bool (true/false)\n - StateSelector animator Int(2/0)")]
        public bool startingState;
        public bool negateStates;
        
        public Element SpawnElement() {
            return new LogicReceiverAction();
        }

        public bool IsMine(Element element) {
            return element is LogicReceiverAction;
        }
    }
}
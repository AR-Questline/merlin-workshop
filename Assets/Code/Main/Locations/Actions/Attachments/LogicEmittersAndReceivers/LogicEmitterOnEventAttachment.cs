using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [DisallowMultipleComponent]
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Emits logic to target locations when an event occurs.")]
    public class LogicEmitterOnEventAttachment : MonoBehaviour, IAttachmentSpec {
        public bool separateEvents;
        [HideIf(nameof(separateEvents))]
        public EmitLogicTargetState targetState;
        [HideIf(nameof(separateEvents)), Indent]
        public bool once = true;

        [LabelText("@separateEvents ? \"Event to Enable From\" : \"Event to Trigger From\""), Indent]
        [ValidateInput("@!separateEvents || eventToDisableFrom != eventToTriggerFrom", "Event to Enable From must be different from Event to Trigger From")]
        public LogicEmitterEvent eventToTriggerFrom;

        [ShowIf(nameof(separateEvents)), Indent]
        [ValidateInput("@eventToDisableFrom != eventToTriggerFrom", "Event to Disable From must be different from Event to Trigger From")]
        public LogicEmitterEvent eventToDisableFrom;
        [SerializeField] List<LocationReference> targetsToTriggerOn;

        public IEnumerable<Location> Locations => targetsToTriggerOn.SelectMany(t => t.MatchingLocations(null));
        
        public Element SpawnElement() {
            return new LogicEmitterOnEvent();
        }

        public bool IsMine(Element element) {
            return element is LogicEmitterOnEvent;
        }
        
        public enum EmitLogicTargetState : byte {
            Toggle = 0,
            Enabled = 1,
            Disabled = 2
        }
    }
}
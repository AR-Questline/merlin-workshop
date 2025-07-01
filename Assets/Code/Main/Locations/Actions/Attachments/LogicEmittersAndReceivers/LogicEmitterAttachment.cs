using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Adds logic emitter to the location interaction (f.e. lever). They send signals to Logic Receivers.")]
    public class LogicEmitterAttachment : LogicEmitterAttachmentBase {
        [InfoBox("If UseInteraction will Interact with location")]
        [FoldoutGroup("Interaction System"), Tooltip("Will interact with Location (just like Hero would)")]
        public bool useInteraction;
        [FoldoutGroup("Interaction System")]
        public bool isIllegal;
        [FoldoutGroup("Interaction System"), ShowIf(nameof(isIllegal))]
        public float bounty = 100f;

        [InfoBox("If UseStates will send: Activated event with true or false value")]
        [FoldoutGroup("Interaction System"), Tooltip("Will send events to Logic Receiver (gates etc)")]
        public bool useStates = true;
        [FoldoutGroup("Interaction System"), Indent, ShowIf(nameof(useStates))]
        public bool negateVisualState;
        [FoldoutGroup("Interaction System"), Indent, Tooltip("Changes default state of this Emitter (not Receiver)"), ShowIf(nameof(useStates))]
        public bool initialState;
        [FoldoutGroup("Interaction System"), Indent, ShowIf(nameof(useStates))]
        public bool changeStates;
        [FoldoutGroup("Interaction System"), Indent, Tooltip("If Receiver is activated, this Emitter is also set to active state, even if other Emitter activated the Receiver"), ShowIf(nameof(ShowReceiverListener))]
        public bool syncWithReceiver = true;
        
        //Active System
        [InfoBox("Can the Emitter be in Active and Inactive state.\nIf it's Inactive it will send: FailedActivated event instead")]
        [FoldoutGroup("Activity System"), Tooltip("Can the button be disabled and send \"failed\" events (like stuck doors or wyrdbridge not showing)")]
        public bool useActiveSystem;
        [FoldoutGroup("Activity System"), Indent, ShowIf(nameof(useActiveSystem))]
        public bool initialActive;
        [FoldoutGroup("Activity System"), Indent, ShowIf(nameof(useActiveSystem))]
        public bool activeOnFlag;
        [FoldoutGroup("Activity System"), Indent, ShowIf(nameof(ShowActivationFlag)), Tags(TagsCategory.Flag)]
        public string activationFlag;
        
        bool ShowReceiverListener => changeStates && useStates;
        bool ShowActivationFlag => activeOnFlag && useActiveSystem;
        protected override bool ShowInactiveInteractionSound => useActiveSystem;
        
        public override Element SpawnElement() {
            return new LogicEmitterAction();
        }

        public override bool IsMine(Element element) {
            return element is LogicEmitterAction;
        }
    }
}
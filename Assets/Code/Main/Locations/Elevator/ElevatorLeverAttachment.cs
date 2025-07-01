using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.List;
using Sirenix.OdinInspector;
using System;
using Awaken.TG.Main.Templates.Attachments;

namespace Awaken.TG.Main.Locations.Elevator {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Adds interaction that moves elevator to the next position.")]
    public class ElevatorLeverAttachment : LogicEmitterAttachmentBase {
        [ListDrawerSettings(AddCopiesLastElement = false), List(ListEditOption.FewButtons | ListEditOption.NullNewElement)]
        public LocationSpec[] locationSpecsReferences = Array.Empty<LocationSpec>();

        protected override bool ShowInactiveInteractionSound => true;

        public override Element SpawnElement() {
            return new ElevatorLeverAction();
        }

        public override bool IsMine(Element element) {
            return element is ElevatorLeverAction;
        }
    }
}
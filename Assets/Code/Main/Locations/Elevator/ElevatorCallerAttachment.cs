using System;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Adds interaction that calls elevator.")]
    public class ElevatorCallerAttachment : LogicEmitterAttachmentBase {
        public Transform targetPoint;
        public GameObject navmeshCutObject;

        protected override bool ShowInactiveInteractionSound => true;

        public override Element SpawnElement() {
            return new ElevatorCallerAction();
        }

        public override bool IsMine(Element element) {
            return element is ElevatorCallerAction;
        }

        [Button]
        void AddLocationReferenceFromParent() {
            if (locations.locationSpecsReferences.IsNullOrEmpty()) {
                locations.locationSpecsReferences = new LocationSpec[1];
            } else if (locations.locationSpecsReferences[^1] != null) {
                Array.Resize(ref locations.locationSpecsReferences, locations.locationSpecsReferences.Length + 1);
            }

            locations.locationSpecsReferences[^1] = transform.parent.GetComponentInParent<LocationSpec>();
        }
    }
}
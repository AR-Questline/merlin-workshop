using System;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Cycles location states, triggered by Location State Change.")]
    public class LocationStatesAttachment : MonoBehaviour, IAttachmentSpec {
        [InfoBox("States are used by animator and attachment groups,\nname of a state should be the same as the name of attachment group,\nstate number should be the same as 'State' parameter in animator.")]
        [SerializeField] LocationState[] states = Array.Empty<LocationState>();
        [SerializeField] int startingState;

        public LocationState[] States => states;
        public int StartingState => startingState;

        public Element SpawnElement() {
            return new LocationStatesElement();
        }
        
        public bool IsMine(Element element) => element is LocationStatesElement;
    }

    [Serializable]
    public struct LocationState {
        public string name;
        public bool interactOnStart;
    }
}
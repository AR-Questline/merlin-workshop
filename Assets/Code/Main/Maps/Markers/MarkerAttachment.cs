using System;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Maps.Markers {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Defines custom map markers for location.")]
    public class MarkerAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeReference] IMarkerDataWrapper markerDataWrapper;

        public MarkerData MarkerData => markerDataWrapper.MarkerData;
        public IMarkerDataWrapper MarkerDataWrapper => markerDataWrapper;
        
        public Element SpawnElement() {
            if (markerDataWrapper == null) {
                var exception = new NullReferenceException($"{nameof(markerDataWrapper)} in {nameof(MarkerAttachment)} attached to {gameObject.name} is null");
                Debug.LogException(exception);
                return null;
            }
            return markerDataWrapper.CreateMarker();
        }

        public bool IsMine(Element element) => element is LocationMarker;
    }
}
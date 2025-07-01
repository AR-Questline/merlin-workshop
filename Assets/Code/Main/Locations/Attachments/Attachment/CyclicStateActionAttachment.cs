using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Ensures proper state of Logic Emitters at specific times.")]
    public class CyclicStateActionAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, DisableInPlayMode, Tooltip("This Logic Emitters will have their state changed to match the cyclic states list")]
        LocationReference locationsWithEmitters;
        [SerializeField] CyclicStateData cyclicStatesData;

        public IEnumerable<Location> LocationsWithEmitters => locationsWithEmitters.MatchingLocations(null);
        public CyclicStateDatum[] CyclicStates => cyclicStatesData.SortedCyclicStates;

        // === Operations
        public Element SpawnElement() => new CyclicStateAction();

        public bool IsMine(Element element) {
            return element is CyclicStateAction;
        }
    }

    [Serializable]
    public struct CyclicStateData {
        public CyclicStateDatum[] cyclicStates;
        public CyclicStateDatum[] SortedCyclicStates => cyclicStates.OrderBy(s => s.GetTime()).ToArray();
    }

    [Serializable]
    public struct CyclicStateDatum {
        [SerializeField] ARTimeOfDay arTimeOfDay;
        [SerializeField] bool state;

        public bool State => state;
        public TimeSpan GetTime() => arTimeOfDay.GetTime();
    }
}
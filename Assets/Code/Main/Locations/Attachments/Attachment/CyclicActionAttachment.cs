using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Activates logic emitters cyclically.")]
    public class CyclicActionAttachment : MonoBehaviour, IAttachmentSpec {
        // === Serialized
        [SerializeField, FoldoutGroup("Actions"), Tooltip("If true locations can be divided into 2 groups: " +
                                                          "activated always even if on another scene (they will be activated when this location becomes active) " +
                                                          "and activated only when player is witnessing the cycle change (this location is active)")] 
        bool checkIfWitnessingTheCycle;
        [SerializeField, FoldoutGroup("Actions"), DisableInPlayMode, ShowIf(nameof(CheckIfWitnessingTheCycle)), Tooltip("This locations will be interacted only when witnessing the cycle")]
        LocationReference locationsActivatedWhenWitnessing;
        [SerializeField, FoldoutGroup("Actions"), Tooltip("If true locations below can be activated multiple times on restore, once per each interval that happened unwitnessed")] 
        bool canNecessaryActionsBePerformedMultipleTimesOnRestore;
        [SerializeField, FoldoutGroup("Actions"), DisableInPlayMode, Tooltip("This locations will be interacted with every cycle")] 
        LocationReference locationsAlwaysActivated;
        [SerializeField, FoldoutGroup("Time Cycle"), Range(0,23), Tooltip("Default hour we'll start the cycle count, " +
                                                                          "if cycle is daily and the hours is set to 12, it will activate every day at 12")] 
        int hour;
        [SerializeField, FoldoutGroup("Time Cycle"), Range(0,59), Tooltip("Default minute we'll start the cycle count, " +
                                                                          "if cycle is hourly and the minutes is set to 0, it will activate every round hour")] 
        int minutes;
        [SerializeField, FoldoutGroup("Time Cycle")] ARTimeSpan actionInterval;

        public bool CheckIfWitnessingTheCycle => checkIfWitnessingTheCycle;
        public IEnumerable<Location> LocationsActivatedWhenWitnessing => locationsActivatedWhenWitnessing.MatchingLocations(null);
        public bool CanNecessaryActionsBePerformedMultipleTimesOnRestore => canNecessaryActionsBePerformedMultipleTimesOnRestore;
        public IEnumerable<Location> LocationsAlwaysActivated => locationsAlwaysActivated.MatchingLocations(null);
        public int Hour => hour;
        public int Minutes => minutes;
        public ARTimeSpan ActionInterval => actionInterval;

        // === Operations
        public Element SpawnElement() => new CyclicAction();

        public bool IsMine(Element element) {
            return element is CyclicAction;
        }
    }
}
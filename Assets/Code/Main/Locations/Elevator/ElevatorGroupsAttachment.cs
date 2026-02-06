using System;
using System.Linq;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Locations.Elevator {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Manages state of multiple elevators for synchronized movement.")]
    public class ElevatorGroupsAttachment : LogicEmitterAttachmentBase {
        [ValidateInput(nameof(EDITOR_IsCycleDurationValid), "Cycle duration must be greater than the maximum cycle time defined in elevatorCycleData.")]
        public float cycleDuration = 10f;
        public int cycleRepeats = 0;
        public ElevatorCycleData[] elevatorCycleData = Array.Empty<ElevatorCycleData>();

        [ShowInInspector] float EDITOR_MaxCycleTime => elevatorCycleData.Length > 0 
                                ? elevatorCycleData.Max(d => d.states.Length > 0 
                                    ? d.states.Max(s => s.time)
                                    : 0) 
                                : 0;
        protected override bool ShowInactiveInteractionSound => false;

        public override Element SpawnElement() => new ElevatorGroups();

        public override bool IsMine(Element element) => element is ElevatorGroups;

        bool EDITOR_IsCycleDurationValid() {
            return cycleDuration >= EDITOR_MaxCycleTime;
        }
    }
    
    [Serializable]
    public struct ElevatorCycleData {
        public LocationSpec locationSpec;
        public int defaultFloor;
        public float delayReset;
        public ElevatorState[] states;

        [Serializable]
        public struct ElevatorState {
            public float time;
            public int targetFloor;
            public bool otherFloorAtFirstCycle;
            [ShowIf(nameof(otherFloorAtFirstCycle))] public int targetFloorAtFirstCycle;

            public int TargetFloorAtFirstCycle => otherFloorAtFirstCycle ? targetFloorAtFirstCycle : targetFloor;
        }
    }
}
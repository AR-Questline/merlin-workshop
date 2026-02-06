using System.Linq;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Shift Position"), NodeSupportsOdin]
    public class SEditorShiftLocationPosition : EditorStep {
        public LocationReference locations;
        [Space]
        public float duration = 1f;
        public Vector3 newPosition;
        [Indent, LabelWidth(100)] public bool relativeShift = true;
        [LabelWidth(120)]
        public bool waitForCompletion = true;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SShiftLocationPosition {
                locations = locations,
                newPosition = newPosition,
                duration = duration,
                waitForCompletion = waitForCompletion,
                relativeShift = relativeShift
            };
        }
    }

    public partial class SShiftLocationPosition : StoryStep {
        public LocationReference locations;
        public Vector3 newPosition;
        public float duration;
        public bool waitForCompletion;
        public bool relativeShift;
        
        public override StepResult Execute(Story story) {
            var stepResult = new StepResult();
            ShiftLocationsPosition(story, stepResult).Forget();
            if (!waitForCompletion) {
                stepResult.Complete();
            }
            return stepResult;
        }

        async UniTaskVoid ShiftLocationsPosition(Story story, StepResult stepResult) {
            var targetLocations = locations.MatchingLocations(story).ToList();
            if (targetLocations.Count == 0) {
                stepResult.Complete();
                return;
            }
            
            var movingElements = new System.Collections.Generic.List<MovingLocation>(targetLocations.Count);
            foreach (var loc in targetLocations) {
                var initialPosition = loc.Coords;
                var finalPosition = relativeShift ? initialPosition + newPosition : newPosition;
                var movingElement = loc.AddElement(new MovingLocation(initialPosition, finalPosition, duration));
                movingElements.Add(movingElement);
            }
            
            while (true) {
                bool anyMoving = false;
                for (int i = 0; i < movingElements.Count; i++) {
                    if (!movingElements[i].HasBeenDiscarded) {
                        anyMoving = true;
                        break;
                    }
                }
                if (!anyMoving) {
                    break;
                }
                await UniTask.Yield();
            }
            
            stepResult.Complete();
        }
    }
}
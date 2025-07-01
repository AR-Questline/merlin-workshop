using System;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using GameAnalyticsSDK;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Analytics: Send Event")]
    public class SEditorAnalyticEvent : EditorStep {

        [Tooltip("Progression events are for events that are started and can be completed/failed. Design events are for everything else.")]
        public SAnalyticEvent.EventType evtType = SAnalyticEvent.EventType.Design;

        [ShowIf(nameof(IsProgression))]
        public GAProgressionStatus progressionType = GAProgressionStatus.Start;
        
        [Tooltip("There should be at most 3 parts of design event.")]
        public string[] parts = Array.Empty<string>();
        [Tooltip("This one is optional.")]
        public float value;

        bool IsProgression => evtType == SAnalyticEvent.EventType.Progression;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SAnalyticEvent {
                evtType = evtType,
                progressionType = progressionType,
                parts = parts,
                value = value
            };
        }
    }
    
    public partial class SAnalyticEvent : StoryStep {
        public EventType evtType;
        public GAProgressionStatus progressionType;
        public string[] parts = Array.Empty<string>();
        public float value;
        
        public override StepResult Execute(Story story) {
#if !UNITY_GAMECORE && !UNITY_PS5
            if (parts == null || parts.Length == 0 || parts.Length > 3) {
                Log.Important?.Error("Number of parts needs to be in 1-3 range.");
                return StepResult.Immediate;
            }

            string eventName = string.Join(":", parts);
            if (evtType == EventType.Design) {
                //GameAnalytics.NewDesignEvent(eventName, value);
            } else {
                if (parts.Length == 1) {
                    //GameAnalytics.NewProgressionEvent(progressionType, parts[0], (int)value);
                } else if (parts.Length == 2) {
                    //GameAnalytics.NewProgressionEvent(progressionType, parts[0], parts[1], (int) value);
                } else if (parts.Length == 3) {
                    //GameAnalytics.NewProgressionEvent(progressionType, parts[0], parts[1], parts[2], (int) value);
                }
            }
#endif
            
            return StepResult.Immediate;
        }

        public enum EventType {
            Design = 0,
            Progression = 1,
        }
    }
}
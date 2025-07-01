using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.General;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Data.Attachment {
    [Serializable]
    public struct InteractionIntervalData {
        const string TimeGroup = "Time";
        const string ActionGroup = "Action";
        const string OverridesGroup = "Overrides";
        
        public string name;
        
        [SerializeField, BoxGroup(TimeGroup)] int startHour;
        [SerializeField, BoxGroup(TimeGroup)] int startMinutes;
        [SerializeField, BoxGroup(TimeGroup)] int startDeviation;
        [SerializeField, BoxGroup(TimeGroup), DisableIf(nameof(HasScene))] bool subdivide;
        [SerializeField, BoxGroup(TimeGroup), ShowIf(nameof(subdivide))] int intervalMinutes;
        [SerializeField, BoxGroup(TimeGroup), ShowIf(nameof(subdivide))] int intervalDeviation;

        // TODO: replace whole ActionGroup with valid InteractionData
        InteractionData _data;
        
        [SerializeField, BoxGroup(ActionGroup)] InteractionData.IdleType type;

        [SerializeField, BoxGroup(ActionGroup), ShowIf(nameof(HasPosition))] IdlePosition position;
        [SerializeField, BoxGroup(ActionGroup), ShowIf(nameof(HasForward))] IdlePosition forward;
        [SerializeField, BoxGroup(ActionGroup), ShowIf(nameof(HasRange))] float range;
        [SerializeField, BoxGroup(ActionGroup), ShowIf(nameof(HasTags)), Tags(TagsCategory.Interaction)] string interactionTag;

        [SerializeField, BoxGroup(ActionGroup), ShowIf(nameof(HasAllowInteractionRepeat))] bool allowInteractionRepeat;
        [SerializeField, BoxGroup(ActionGroup), ShowIf(nameof(HasWaitTime))] FloatRange waitTime;
        [SerializeField, BoxGroup(ActionGroup), ShowIf(nameof(HasUniqueID)), Tags(TagsCategory.InteractionID)] string uniqueID;
        [SerializeField, BoxGroup(ActionGroup), ShowIf(nameof(HasScene))] SceneReference scene;

        [SerializeField, BoxGroup(OverridesGroup), LabelText("In Rain")] bool hasInRainAction;
        [SerializeField, BoxGroup(OverridesGroup), ShowIf(nameof(hasInRainAction))] string inRainAction;
        
        public bool HasPosition => type is InteractionData.IdleType.Stand or InteractionData.IdleType.Wander or InteractionData.IdleType.Interactions;
        public bool HasForward => type is InteractionData.IdleType.Stand;    
        public bool HasRange => type is InteractionData.IdleType.Wander or InteractionData.IdleType.Interactions;
        public bool HasTags => type is InteractionData.IdleType.Interactions;
        public bool HasAllowInteractionRepeat => type is InteractionData.IdleType.Interactions;
        public bool HasWaitTime => type is InteractionData.IdleType.Wander;
        public bool HasUniqueID => type is InteractionData.IdleType.Unique;
        public bool HasScene => type is InteractionData.IdleType.ChangeScene;

        // TODO: temporary filing data with serialized fields. To remove when replaced serialized field with struct
        public void CacheData() {
            _data = new InteractionData(type, position, forward, range, interactionTag, allowInteractionRepeat, waitTime, uniqueID, scene);
        }
        public void FlushData() {
            type = _data.type;
            position = _data.position;
            forward = _data.forward;
            range = _data.range;
            interactionTag = _data.interactionTag;
            allowInteractionRepeat = _data.allowInteractionRepeat;
            waitTime = _data.waitTime;
            uniqueID = _data.uniqueID;
            scene = _data.scene;
        }
        
        public void AppendIntervals(IdleDataElement dataElement, List<InteractionInterval> intervals, in InteractionIntervalData next, bool addDay) {
            var finder = CreateFinder(dataElement);
            
            if (!subdivide) {
                intervals.Add(new InteractionInterval(startHour, startMinutes, startDeviation,
                    new InteractionSource(finder, dataElement.FallbackInteractionData),
                    hasInRainAction ? dataElement.GetCustomAction(inRainAction) : null 
                ));
                return;
            }
            
            int startTime = startHour * 60 + startMinutes;
            int endTime = (next.startHour + (addDay ? 24 : 0)) * 60 + next.startMinutes;
            int duration = endTime - startTime;
            int count = Mathf.CeilToInt(duration / (float) intervalMinutes);
            int actualIntervalMinutes = duration / count;
            int actualIntervalDeviation = Mathf.Min(intervalDeviation, actualIntervalMinutes / 2);

            intervals.EnsureCapacity(intervals.Count + count);
            for (int i = 0; i < count; i++) {
                int intervalStartTime = startTime + actualIntervalMinutes * i;
                int intervalStartHour = (intervalStartTime / 60) % 24;
                int intervalStartMinutes = intervalStartTime % 60;
                intervals.Add(new InteractionInterval(intervalStartHour, intervalStartMinutes, actualIntervalDeviation,
                    new InteractionSource(finder, dataElement.FallbackInteractionData),
                    hasInRainAction ? dataElement.GetCustomAction(inRainAction) : null 
                ));
            }
        }
        
        public IInteractionFinder CreateFinder(IdleDataElement element) {
            CacheData();
            return _data.CreateFinder(element);
        }

        public static InteractionIntervalData StandOnSpawn() => new() {
            name = "StandOnSpawn",
            type = InteractionData.IdleType.Stand,
            position = IdlePosition.NpcSpawn,
            forward = IdlePosition.NpcSpawn,
        };

        public static InteractionIntervalData Interactions() => new() {
            name = "Interactions",
            type = InteractionData.IdleType.Interactions,
            position = IdlePosition.Self,
            range = 10,
        };

        public static int CompareDate(InteractionIntervalData lhs, InteractionIntervalData rhs) {
            return InteractionInterval.CompareDate(lhs.startHour, lhs.startMinutes, rhs.startHour, rhs.startMinutes);
        }

        public struct EDITOR_Accessor {
            public int StartHours(ref InteractionIntervalData data) => data.startHour;
            public int StartMinutes(ref InteractionIntervalData data) => data.startMinutes;
            public ref InteractionData Data(ref InteractionIntervalData data) => ref data._data;
        }
    }
}
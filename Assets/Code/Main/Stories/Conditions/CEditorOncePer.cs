using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Once Per X")]
    public class CEditorOncePer : EditorCondition, IOncePer {
        [HideInInspector] 
        public string flag;
        [NodeEnum]
        public TimeSpans span = TimeSpans.Ever;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new COncePer {
                flag = flag,
                span = span,
            };
        }

        string IOncePer.SpanFlag {
            get => flag;
            set => flag = value;
        }
        TimeSpans IOncePer.Span => span;
    }

    public partial class COncePer : StoryCondition, IOncePer {
        public string flag;
        public TimeSpans span = TimeSpans.Ever;

        public override bool Fulfilled(Story story, StoryStep step) {
            return StoryUtilsRuntime.OncePer(story, this);
        }

        string IOncePer.SpanFlag {
            get => flag;
            set => flag = value;
        }
        TimeSpans IOncePer.Span => span;
    }
}
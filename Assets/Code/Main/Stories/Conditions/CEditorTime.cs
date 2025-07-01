using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.Utility.Times;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Time: From To Check"), NodeSupportsOdin]
    public class CEditorTime : EditorCondition {
        public ARTimeOfDayInterval interval;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CTime {
                interval = new ARTimeOfDayIntervalRuntime(interval),
            };
        }
    }

    public partial class CTime : StoryCondition {
        public ARTimeOfDayIntervalRuntime interval;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return interval.Contains(World.Only<GameRealTime>().WeatherTime.Date.TimeOfDay);
        }
    }
}
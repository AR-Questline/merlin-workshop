using Awaken.TG.Main.Localization;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    public class TrackerDisplayPattern : RichEnum {
        LocString Pattern { get; }

        public string LocalizedPattern => Pattern.ID == null ? string.Empty : Pattern;

        [UnityEngine.Scripting.Preserve]
        public static readonly TrackerDisplayPattern
            None = new(null, nameof(None)),
            Cur = new(LocTerms.TrackerPatternCur, nameof(Cur)), // {cur}
            CurByMax = new(LocTerms.TrackerPatternCurByMax, nameof(CurByMax)), // {cur}/{max}
            ItemCurByMax = new(LocTerms.TrackerPatternItemCurByMax, nameof(ItemCurByMax)); // {item} {cur}/{max};

        protected TrackerDisplayPattern(string displayPatternId, string enumName, string inspectorCategory = "") : base(enumName, inspectorCategory) {
            Pattern = new LocString {
                ID = displayPatternId
            };
        }
    }
}
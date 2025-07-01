using Awaken.TG.Main.Localization;
using Awaken.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Items {
    public class ItemQuality : RichEnum {
        public int Priority { get; }
        public LocString DisplayName { get; }
        public ARColor BgColor { get; }
        public ARColor NameColor { get; }

        [UnityEngine.Scripting.Preserve]
        public static readonly ItemQuality
            Story = new(nameof(Story), 0, LocTerms.QStory, ARColor.QualityStory, ARColor.QualityStoryText),
            Garbage = new(nameof(Garbage), 1, LocTerms.QGarbage, ARColor.QualityGarbage, ARColor.QualityGarbage),
            Tool = new(nameof(Tool), 2, LocTerms.QTool, ARColor.QualityNormal, ARColor.MainWhite),
            Weak = new(nameof(Weak), 3, LocTerms.QWeak, ARColor.QualityGarbage, ARColor.QualityGarbage),
            Normal = new(nameof(Normal), 4, LocTerms.QNormal, ARColor.QualityNormal, ARColor.MainWhite),
            Magic = new(nameof(Magic), 5, LocTerms.QMagic, ARColor.QualityMagic, ARColor.QualityMagicText),
            Quest = new(nameof(Quest), 6, LocTerms.QQuest, ARColor.QualityQuest, ARColor.QualityQuest);

        ItemQuality(string enumName, int priority, string displayName, ARColor bgColor, ARColor nameColor) : base(enumName) {
            Priority = priority;
            DisplayName = new LocString {ID = displayName};
            BgColor = bgColor;
            NameColor = nameColor;
        }
        
        public static int MaxPriority => Quest.Priority;
        
        public static implicit operator int(ItemQuality quality) => quality.Priority;

        public override int CompareTo(RichEnum other) {
            if (other is ItemQuality otherQuality) {
                return Priority.CompareTo(otherQuality.Priority);
            }
            return base.CompareTo(other);
        }
    }
}
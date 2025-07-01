using System;
using System.Diagnostics;

namespace Awaken.TG.Main.Localization {
    public enum Category : byte {
        [UnityEngine.Scripting.Preserve] None = 0,
        Journal = 1,
        Interaction = 2,
        QuestTracker = 3,
        CharacterCreator = 4,
        Quest = 5,
        Item = 6,
        Actor = 7,
        UI = 8,
        Event = 9,
        Fishing = 10,
        Housing = 11,
        Mount = 12,
        Dialogue = 13,
        [UnityEngine.Scripting.Preserve] Effect = 14,
        Status = 15,
        VideoSubtitles = 16,
        Skill = 17,
        Tutorial = 18,
        Faction = 19,
        Location = 20,
    }
    
    /// <summary>
    /// If used inside a commonly-used serializable class/struct, it should be handled from it's PropertyDrawer to determine which category to use.
    /// See <see cref="OptionalLocString"/> and LocStringDrawer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property), Conditional("UNITY_EDITOR")]
    public class LocStringCategoryAttribute : Attribute {
        public Category Category { get; }

        public LocStringCategoryAttribute(Category category) {
            Category = category;
        }
    }
}
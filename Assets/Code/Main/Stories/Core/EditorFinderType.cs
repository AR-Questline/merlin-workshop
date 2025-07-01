using System;

namespace Awaken.TG.Main.Stories.Core {
    [Flags]
         public enum EditorFinderType {
             [UnityEngine.Scripting.Preserve] None = 0,
             QuestModification = 1 << 0,
             FlagUsage = 1 << 1,
             StepUsage = 1 << 2,
             ActorUsage = 1 << 3,
             STextActorChecker = 1 << 4,
             BookmarkUsage = 1 << 5,
             StoryNodeTasks = 1 << 6,
             StoryTextSearch = 1 << 7,
             MissingVoiceOversFinder = 1 << 8,
         }
}
using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Templates;
using QFSW.QC;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCStoryTools {
        [Command("story.start", "Starts a story from the beginning", allowWhiteSpaces: true)]
        [UnityEngine.Scripting.Preserve]
        static void StartStory(string guid, int chapterIndex = 0) {
            if (!Guid.TryParse(guid, out _)) {
                Log.Important?.Error($"Invalid GUID format: {guid}. Please provide a valid GUID.");
                return;
            }

            var storyRuntime = StoryGraphRuntime.Get(guid);
            if (!storyRuntime.HasValue) {
                Log.Important?.Error($"Story graph runtime not found for GUID: {guid}. Use 'story.list' to see available stories.");
                return;
            }

            var hero = Hero.Current;
            if (!hero) {
                Log.Important?.Error("Hero not found");
                storyRuntime.Value.Dispose();
                return;
            }

            var templateRef = new TemplateReference(guid);
            var bookmark = StoryBookmark.ToInitialChapter(templateRef);
            var config = StoryConfig.Base(bookmark, typeof(VDialogue));
            var chapters = storyRuntime.Value.chapters;

            if (chapterIndex > 0 && chapters.Length <= chapterIndex) {
                QuantumConsole.Instance.LogToConsoleAsync($"Chapter index {chapterIndex} is out of range. Starting from the first chapter.");
                chapterIndex = 0;
            }
            
            var story = Story.StartStory(config);
            if (story != null) {
                if (chapterIndex != 0) {
                    var chapterToJumpTo = storyRuntime.Value.chapters[chapterIndex];
                    story.JumpTo(chapterToJumpTo);
                }
                QuantumConsole.Instance.LogToConsoleAsync($"Started story of GUID: {guid} from chapter: {chapterIndex})");
            } else {
                Log.Important?.Error($"Failed to start story with GUID: {guid} from chapter: {chapterIndex}");
            }

            storyRuntime.Value.Dispose();
        }
    }
}
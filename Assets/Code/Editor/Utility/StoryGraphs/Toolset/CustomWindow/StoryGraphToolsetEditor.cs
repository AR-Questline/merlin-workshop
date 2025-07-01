using Sirenix.OdinInspector.Editor;
using UnityEditor;
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset.CustomWindow {
    // public class StoryGraphToolsetEditor : OdinMenuEditorWindow {
    //     const string WindowTitle = "Story Graph Toolset";
    //
    //     QuestModificationFinder _questModificationFinder = new();
    //     VariableModificationFinder _variableModificationFinder = new();
    //     FlagUsageFinder _flagUsageFinder = new();
    //     PresenceChangeFinder _presenceChangeFinder = new();
    //     StepUsageFinder _stepUsageFinder = new();
    //     ActorUsageFinder _actorUsageFinder = new();
    //     STextActorChecker _sTextActorChecker = new();
    //     MissingVoiceOversFinder _missingVOFinder = new();
    //     BookmarkUsageFinder _bookmarkUsageFinder = new();
    //     StoryNodeTasksFinder _storyNodeTasksFinder = new();
    //     StoryTextSearchFinder _storyTextSearchFinder = new();
    //     MultiActorFinder _multiActorFinder = new();
    //     
    //     [MenuItem("TG/Graphs/Story Graph Toolset")]
    //     public static void OpenWindow() {
    //         StoryGraphToolsetEditor window = CreateWindow<StoryGraphToolsetEditor>(WindowTitle);
    //         window.Show();
    //     }
    //
    //     public static void ShowWindow() {
    //         GetWindow<StoryGraphToolsetEditor>(WindowTitle);
    //     }
    //
    //     protected override OdinMenuTree BuildMenuTree() {
    //         return new OdinMenuTree(false) {
    //             { "Quest modification", _questModificationFinder },
    //             { "Variable modification", _variableModificationFinder },
    //             { "Flag usage", _flagUsageFinder },
    //             { "Presence change", _presenceChangeFinder},
    //             { "Step usage", _stepUsageFinder },
    //             { "Actor usage", _actorUsageFinder },
    //             { "Check NONE Actor in SText", _sTextActorChecker },
    //             { "Missing voice overs finder", _missingVOFinder },
    //             { "Bookmark usage", _bookmarkUsageFinder },
    //             { "Find notes and tasks", _storyNodeTasksFinder },
    //             { "Search for text", _storyTextSearchFinder },
    //             { "Multi Actor usage", _multiActorFinder },
    //         };
    //     }
    // }
}
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Stories;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class StartDialogueWithHero : ARUnit {
        ARValueInput<Location> _location;
        ARValueInput<string> _bookmarkName;
        ARValueInput<bool> _useRangeCheck;

        protected override void Definition() {
            _location = RequiredARValueInput<Location>("location");
            _bookmarkName = FallbackARValueInput<string>("bookmark", _ => null);
            _useRangeCheck = FallbackARValueInput<bool>("useRangeCheck", _ => false);

            DefineSimpleAction(flow => {
                Location location = _location.Value(flow);
                string bookmarkName = _bookmarkName.Value(flow);
                bool useRangeCheck = _useRangeCheck.Value(flow);
                DialogueAction dialogueAction = location?.TryGetElement<DialogueAction>();
                if (location == null || dialogueAction == null) {
                    Log.Important?.Error($"This location: {location?.ID} doesn't have DialogueAction! Can't start dialogue with it");
                    return;
                }

                var bookmark = string.IsNullOrWhiteSpace(bookmarkName)
                    ? dialogueAction.Bookmark
                    : StoryBookmark.ToSpecificChapter(dialogueAction.Bookmark.story, bookmarkName);
                dialogueAction.StartDialogue(location, bookmark, useRangeCheck);
            });
        }
    }
}

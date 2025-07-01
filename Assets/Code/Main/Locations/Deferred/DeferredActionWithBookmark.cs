using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Deferred {
    public sealed partial class DeferredActionWithBookmark : DeferredAction {
        public override ushort TypeForSerialization => SavedTypes.DeferredActionWithBookmark;

        [Saved] public StoryBookmark Bookmark { get; private set; }
        [Saved] List<WeakModelRef<Location>> _locationToPass;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        DeferredActionWithBookmark() {}

        public DeferredActionWithBookmark(StoryBookmark bookmark, IEnumerable<DeferredCondition> conditions) : base(conditions) {
            this.Bookmark = bookmark;
        }

        public DeferredActionWithBookmark(StoryBookmark bookmark, IEnumerable<DeferredCondition> conditions, List<WeakModelRef<Location>> locationToPass) 
            : this(bookmark, conditions) {
            _locationToPass = locationToPass;
        }

        public override DeferredSystem.Result TryExecute() {
            StoryConfig config = StoryConfig.Base(Bookmark, null).WithLocations(_locationToPass);
            Story.StartStory(config);
            return DeferredSystem.Result.Success;
        }
    }
}
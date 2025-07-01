using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Stories.Api {
    /// <summary>
    ///     Common events that are issued by any implementor of Story.
    /// </summary>
    public static class StoryEvents {
        public static readonly Event<Story, Story> StoryEnded = new(nameof(StoryEnded));
    }
}
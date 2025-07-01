using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Stories {
    public static class StoryFlags {
        public static class Events {
            static readonly OnDemandCache<string, Event<Hero, string>> UniqueFlagChangedCache = new(
                flag => new Event<Hero, string>($"UniqueFlagChanged/{flag}")
            );

            public static Event<Hero, string> UniqueFlagChanged(string flag) => UniqueFlagChangedCache[flag];
            public static readonly Event<Hero, string> FlagChanged = new(nameof(FlagChanged));
        }

        public static void Set(string flag, bool value) {
            World.Services.Get<GameplayMemory>().Context().Set(flag, value);
            Hero.Current.Trigger(Events.UniqueFlagChanged(flag), flag);
            Hero.Current.Trigger(Events.FlagChanged, flag);
        }

        public static bool Get(string flag) {
            return World.Services.Get<GameplayMemory>().Context().Get<bool>(flag);
        }
    }
}
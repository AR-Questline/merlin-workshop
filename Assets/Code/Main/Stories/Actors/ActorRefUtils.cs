using System;

namespace Awaken.TG.Main.Stories.Actors {
    public static class ActorRefUtils {
        public static bool IsHeroGuid(string actorGuid) {
            return IsSameActor(actorGuid, DefinedActor.Hero.ActorGuid);
        }

        public static bool IsNoneGuid(string actorGuid) {
            return string.IsNullOrWhiteSpace(actorGuid) || IsSameActor(actorGuid, DefinedActor.None.ActorGuid);
        }

        static bool IsSameActor(string actorGuid1, string actorGuid2) {
            return actorGuid2.Equals(actorGuid1, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
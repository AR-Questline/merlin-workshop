namespace Awaken.TG.Main.Stories.Actors {
    public static class ActorRefExtensions {
        [UnityEngine.Scripting.Preserve]
        public static bool IsHero(in this ActorRef actorRef) {
            return ActorRefUtils.IsHeroGuid(actorRef.guid);
        }

        public static bool IsNone(in this ActorRef actorRef) {
            return ActorRefUtils.IsNoneGuid(actorRef.guid);
        }
    }
}

using System;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Stories.Actors {
    [Serializable]
    public partial struct ActorRef {
        public ushort TypeForSerialization => SavedTypes.ActorRef;

        [Saved] public string guid;
        public static implicit operator string(ActorRef actorRef) => actorRef.guid;
        public readonly Actor Get() => World.Services.Get<ActorsRegister>().GetActor(this);
        public readonly bool IsEmpty => guid.IsNullOrWhitespace();
        public readonly bool Equals(ActorRef other) => guid == other.guid;
    }
}
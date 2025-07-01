using System;
using Awaken.TG.Main.AI.Barks;

namespace Awaken.TG.Main.Stories.Actors {
    public struct Actor : IEquatable<Actor> {
        public string Id { get; private set; }
        public string Name { get; set; }
        public bool ShowNameInDialogue { get; private set; }
        public bool IsSet => !string.IsNullOrWhiteSpace(Id);
        public bool HasName => !string.IsNullOrWhiteSpace(Name);
        public bool HasBarks => BarkConfig is { HasStory: true };
        public BarkConfig BarkConfig { get; }
        public bool IsFake { get; }
        public int FakeIndex { get; }
        
        public Actor(string id, string name, bool showNameInDialogues, BarkConfig barkConfig, bool isFake, int fakeIndex) {
            Id = id;
            Name = name;
            ShowNameInDialogue = showNameInDialogues;
            BarkConfig = barkConfig;
            IsFake = isFake;
            FakeIndex = fakeIndex;
        }
        
        public static implicit operator ActorRef(Actor actor) {
            return new ActorRef { guid = actor.Id };
        }
        
        // === Equality members
        public readonly bool Equals(Actor other) {
            return Id == other.Id;
        }

        public override bool Equals(object obj) {
            return obj is Actor other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Id != null ? Id.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0) ^ (BarkConfig != null ? BarkConfig.GetHashCode() : 0) ;
            }
        }
        
        public static bool operator ==(Actor a, Actor b) => a.Equals(b);
        public static bool operator !=(Actor a, Actor b) => !(a == b);
    }
}
using System.ComponentModel;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Enums.Helpers;

namespace Awaken.TG.MVC.Relations {
    /// <summary>
    /// Represents a single side of a relation. Relations always come in pairs (a parent->children relation
    /// is paired with a child->parent relation), and are created by instantiating a RelationPair.
    /// </summary>
    [TypeConverter(typeof(RelationConverter))]
    public abstract class Relation {
        // === Properties

        public RelationPair Pair { get; }
        public string Name { get; }
        public Arity MyArity { get; }
        public Arity OtherArity { get; }

        internal Relation GenericOpposite => Pair.GetOpposite(this);

        public int Order => Pair.Order;

        // === Events
        
        public struct RelationEvents {
            public HookableEvent<IModel, RelationEventData> BeforeEstablished { get; internal set; }
            public Event<IModel, RelationEventData> AfterEstablished { get; internal set; }
            public HookableEvent<IModel, RelationEventData> BeforeAttached { get; internal set; }
            public Event<IModel, RelationEventData> AfterAttached { get; internal set; }
            public Event<IModel, RelationEventData> BeforeDetached { get; internal set; }
            public Event<IModel, RelationEventData> AfterDetached { get; internal set; }
            public Event<IModel, RelationEventData> BeforeDisestablished { get; internal set; }
            public Event<IModel, RelationEventData> AfterDisestablished { get; internal set; }
            
            public Event<IModel, RelationEventData> Changed { get; internal set; }
        }

        public RelationEvents Events { get; }

        // === Constructors

        protected Relation(RelationPair pair, string name, Arity myArity, Arity otherArity) {
            Pair = pair;
            Name = name;
            MyArity = myArity;
            OtherArity = otherArity;
            Events = new RelationEvents {
                BeforeEstablished = new HookableEvent<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.BeforeEstablished)}"),
                AfterEstablished = new Event<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.AfterEstablished)}"),
                BeforeAttached = new HookableEvent<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.BeforeAttached)}"),
                AfterAttached = new Event<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.AfterAttached)}"),
                BeforeDetached = new Event<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.BeforeDetached)}"),
                AfterDetached = new Event<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.AfterDetached)}"),
                BeforeDisestablished = new Event<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.BeforeDisestablished)}"),
                AfterDisestablished = new Event<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.AfterDisestablished)}"),
                Changed = new Event<IModel, RelationEventData>($"{Name}:{nameof(RelationEvents.Changed)}"),
            };
        }
        
        // === Serialization

        public string Serialize() => StaticStringSerialization.Serialize(Pair.DeclaringType, Name);
        public static Relation Deserialize(string serializedRelation) =>
            StaticStringSerialization.Deserialize<Relation>(serializedRelation);
    }

    /// <inheritdoc />
    public class Relation<TOther> : Relation {
        public Relation(RelationPair pair, string name, Arity myArity, Arity otherArity) 
            : base(pair, name, myArity, otherArity) { }
    }

    /// <inheritdoc />
    public class Relation<TMe, TOther> : Relation<TOther> {
        // === Properties

        [UnityEngine.Scripting.Preserve]
        public Relation<TOther, TMe> Opposite => (Relation<TOther, TMe>)GenericOpposite;

        // === Constructors

        public Relation(RelationPair pair, string name, Arity myArity, Arity otherArity) 
            : base(pair, name, myArity, otherArity) { }

        // === Strings

        public override string ToString() => RelationNaming.SideDescription(this);
    }
}
using System;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Stories.Actors {
    /// <summary>
    /// RichEnum used to define static Actors available in Stories.
    /// </summary>
    public class DefinedActor : RichEnum {
        // === Fields
        readonly Func<Actor> _retrieveFunc;

        // === Definitions
        public static readonly DefinedActor
            None = new(nameof(None), () => default),
            Hero = new(nameof(Hero), () => new Actor(nameof(Hero), Heroes.Hero.Current?.Name, false, null, false, 0));
        
        // === Properties
        // ActorName, ActorGuid and ActorPath in case of defined actors are purposely the same.
        // This is to make it easier to read and understand the code.
        public string ActorName => EnumName;
        public string ActorGuid => EnumName;
        public string ActorPath => EnumName;
        public ActorRef ActorRef => new() { guid = ActorGuid };

        // === Constructors
        DefinedActor(string enumName, Func<Actor> retrieveFunc) : base(enumName) {
            _retrieveFunc = retrieveFunc;
        }
        
        // === Operations
        public Actor Retrieve() => _retrieveFunc();
    }
}
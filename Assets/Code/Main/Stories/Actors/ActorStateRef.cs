using System;

namespace Awaken.TG.Main.Stories.Actors {
    [Serializable]
    public struct ActorStateRef {
        public string stateName;
        
        public static implicit operator string(ActorStateRef stateRef) => stateRef.stateName;
    }
}
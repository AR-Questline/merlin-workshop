using System;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    [Serializable]
    public class FModEventRef : ScriptableObject {
        public EventReference eventPath;

        protected virtual EventReference EventPath => eventPath;

        public static implicit operator EventReference(FModEventRef e) {
            return e.EventPath;
        }
    }
}
using System;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    [Serializable]
    public struct LockpickingAudio {
        public EventReference enterToolsIntoLock;
        [Space]
        public EventReference pickRotate;
        public EventReference pickDamageTaken;
        public EventReference pickBreak;
        [Space]
        public EventReference lockRotateOpen;
        public EventReference lockToNextLayer;
        public EventReference lockOpen;
    }
}
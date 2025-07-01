using System;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace FMODUnity {
    [AddComponentMenu("FMOD Studio/FMOD Studio Event Emitter")]
    public class StudioEventEmitter : MonoBehaviour {
        public EventReference EventReference;

        [Obsolete("Use the EventReference field instead")]
        public string Event = "";

        [FormerlySerializedAs("PlayEvent")]
        public EmitterGameEvent EventPlayTrigger = EmitterGameEvent.None;
        [Obsolete("Use the EventPlayTrigger field instead")]
        public EmitterGameEvent PlayEvent
        {
            get { return EventPlayTrigger; }
            set { EventPlayTrigger = value; }
        }
        [FormerlySerializedAs("StopEvent")]
        public EmitterGameEvent EventStopTrigger = EmitterGameEvent.None;
        [Obsolete("Use the EventStopTrigger field instead")]
        public EmitterGameEvent StopEvent
        {
            get { return EventStopTrigger; }
            set { EventStopTrigger = value; }
        }
        public bool AllowFadeout = true;
        public bool TriggerOnce = false;
        public bool Preload = false;
        [FormerlySerializedAs("AllowNonRigidbodyDoppler")]
        public bool NonRigidbodyVelocity = false;
        public ParamData[] Params = new ParamData[0];
        public bool OverrideAttenuation = false;
        public float OverrideMinDistance = -1.0f;
        public float OverrideMaxDistance = -1.0f;
        [SerializeField] bool IsStatic;
        
        protected virtual void Awake() { }
        protected virtual void OnDestroy() { }
    }
}
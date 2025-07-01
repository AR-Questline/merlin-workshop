using System;
using Awaken.Utility.Collections;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public class InteractionSFX : MonoBehaviour {
        void OnEnable() {
            //GetComponentsInChildren<StudioEventEmitter>().ForEach(e => e.Play());
        }

        void OnDisable() {
            Stop();
        }

        void OnDestroy() {
            Stop();
        }

        void Stop() {
            //GetComponentsInChildren<StudioEventEmitter>().ForEach(e => e.Stop());
        }
    }
}

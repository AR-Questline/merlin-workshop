using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Scenes.SceneConstructors;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.UI.ButtonSystem {
    [Serializable]
    public class PromptAudio : IPromptListener {
        [SerializeField] EventReference keyDownSound;
        [SerializeField] protected EventReference keyUpSound;
        [SerializeField] EventReference tapSound;

        public EventReference KeyDownSound {
            get => keyDownSound;
            init => keyDownSound = value;
        }
        
        public virtual EventReference KeyUpSound {
            get => keyUpSound;
            init => keyUpSound = value;
        }
        
        public EventReference TapSound {
            get => tapSound;
            init => tapSound = value;
        }

        ARFmodEventEmitter _audioEmitter;

        ARFmodEventEmitter AudioEmitter {
            get {
                if (_audioEmitter == null) {
                    return _audioEmitter = CommonReferences.Get.PromptAudioEmitter;
                }

                return _audioEmitter;
            }
        }

        public void OnHoldKeyDown(Prompt source) {
            if (CheckEventReference(KeyDownSound)) {
                //AudioEmitter.PlayNewEventWithPauseTracking(KeyDownSound);
            }
        }

        public virtual void OnHoldKeyUp(Prompt source, bool completed) {
            //AudioEmitter.Stop();
            if (CheckEventReference(KeyUpSound)) {
                FMODManager.PlayOneShot(KeyUpSound);
            }
        }

        public void OnTap(Prompt source) {
            if (CheckEventReference(TapSound)) {
                FMODManager.PlayOneShot(TapSound);
            }
        }
        
        public void OnHoldPromptInterrupted(Prompt source) {
            //AudioEmitter.Stop();
        }

        public void SetName(string name) { }

        public void SetActive(bool active) { }

        public void SetVisible(bool visible) { }

        bool CheckEventReference(in EventReference eventReference) {
            return !eventReference.IsNull;
        }
    }
}
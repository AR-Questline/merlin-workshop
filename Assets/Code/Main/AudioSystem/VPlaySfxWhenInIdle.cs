using Awaken.TG.Main.AudioSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG {
    [NoPrefab]
    public class VPlaySfxWhenInIdle : View<PlaySfxWhenNpcInIdle> {
        ARFmodEventEmitter _emitter;
        
        public override Transform DetermineHost() => Target.ParentModel.MainView.transform;

        
        protected override void OnInitialize() {
            _emitter = gameObject.AddComponent<ARFmodEventEmitter>();
            // _emitter.ChangeEvent(Target.sfxToPlay);
            // _emitter.EventPlayTrigger = EmitterGameEvent.None;
            // _emitter.EventStopTrigger = EmitterGameEvent.ObjectDestroy;
        }

        public void Play() {
            // _emitter.Play();
        }

        public void Stop() {
            // _emitter.Stop();
        }
    }
}
using Awaken.TG.MVC;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public class ARFmodEventEmitter : StudioEventEmitter {
        [SerializeField] bool timeScaleDependent;
        [SerializeField] bool stopOnDestroy = true;

        public bool TimeScaleDependent => timeScaleDependent;
        
        static System.Action<StudioEventEmitter> s_registerToUnityUpdateProvider = RegisterToUnityUpdateProvider;
        
        protected override void Awake() {
            // base.Awake();
            // if (timeScaleDependent) {
            //     onPlayEventWithPauseTracking = s_registerToUnityUpdateProvider;
            // }
        }
        
        static void RegisterToUnityUpdateProvider(StudioEventEmitter thisEmitter) {
            UnityUpdateProvider.GetOrCreate().RegisterStudioEventEmitter(thisEmitter);
        } 

        protected override void OnDestroy() {
            // base.OnDestroy();
            // if (stopOnDestroy && IsPlaying()) {
            //     Stop();
            // }
        }
    }
}

using Awaken.TG.MVC;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    public static class StudioEventEmitterExt {
        public static void UpdatePauseTracking(this StudioEventEmitter emitter) {
            // var instance = emitter.EventInstance;
            // if (!instance.isValid()) {
            //     var updateProvider = World.Services.Get<UnityUpdateProvider>();
            //     updateProvider.UnregisterStudioEventEmitter(emitter);
            //     return;
            // }
            //
            // instance.getPlaybackState(out PLAYBACK_STATE playbackState);
            //
            // if (playbackState == PLAYBACK_STATE.STOPPED) {
            //     var updateProvider = World.Services.Get<UnityUpdateProvider>();
            //     updateProvider.UnregisterStudioEventEmitter(emitter);
            //     return;
            // }
            //
            // instance.getPaused(out var isPaused);
            //
            // if (Time.timeScale == 0 && !isPaused) {
            //     instance.setPaused(true);
            // } else if (Time.timeScale > 0 && isPaused) {
            //     instance.setPaused(false);
            // }
        }
    }
}

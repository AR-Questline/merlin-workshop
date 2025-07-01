using System.Threading;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Unity.Mathematics;

namespace Awaken.TG.Main.Heroes.Audio {
    [SpawnsView(typeof(VNonSpatialVoiceOvers))]
    public partial class NonSpatialVoiceOvers : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        CancellationTokenSource _source;
        
        public ARFmodEventEmitter EventEmitter => View<VNonSpatialVoiceOvers>().EventEmitter;
        VNonSpatialVoiceOvers EmitterView => View<VNonSpatialVoiceOvers>();

        public void PlayOneShot(EventReference eventReference) {
            if (eventReference.IsNull) {
                return;
            }
            //RuntimeManager.PlayOneShotAttached(eventReference, EmitterView.gameObject, EmitterView);
        }

        public async UniTask Play(CancellationTokenSource cancellationTokenSource, int textReadTime, int? cutDuration, bool cancelTokenOnEnd) {
            if (_source is {Token: {CanBeCanceled: true}}) {
                _source.Cancel();
                _source = null;
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }

            if (HasBeenDiscarded || ParentModel.HasBeenDiscarded || View<VNonSpatialVoiceOvers>() == null) {
                return;
            }

            _source = cancellationTokenSource;
            
            var emitter = EventEmitter;
            //emitter.PlayCurrentEventWithPauseTracking();
            int eventDuration = 0;
            // if (RuntimeManager.TryGetEventDescription(EmitterView.EventEmitter.EventReference, out var eventDescription, EmitterView)) {
            //     eventDescription.getLength(out eventDuration);
            // } else {
            //     eventDuration = 0;
            // }
            
            if (eventDuration <= 1050f) {
                eventDuration = math.max(eventDuration, textReadTime);
            } else if (cutDuration.HasValue) {
                eventDuration = math.clamp(eventDuration - cutDuration.Value, 1, eventDuration);
            }
            
            bool ignoreTimescale = !emitter.TimeScaleDependent;

            //var emitterEventInstance = emitter.EventInstance;
            await AsyncUtil.DelayTime(this, eventDuration * 0.001f, ignoreTimescale, cancellationTokenSource);

            // if (emitter.EventInstance.handle == emitterEventInstance.handle) {
            //     emitter.Stop();
            // }
            if (cancelTokenOnEnd && cancellationTokenSource.Token.CanBeCanceled) {
                cancellationTokenSource.Cancel();
            }
        }
    }
}
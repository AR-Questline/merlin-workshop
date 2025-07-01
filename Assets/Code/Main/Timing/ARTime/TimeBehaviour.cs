using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Timing.ARTime {
    public abstract class TimeBehaviour<T> : View<T> where T : TimeModel, new() {
        protected virtual void Start() {
            SpawnModel().Forget();
        }

        async UniTaskVoid SpawnModel() {
            if (World.Services == null) {
                await UniTask.WaitUntil(() => World.Services != null);
            }
            var model = World.Add(new T());
            World.BindView(model, this, true, true);
            model.GetOrCreateTimeDependent()
                .WithUpdate(ProcessUpdate)
                .WithLateUpdate(ProcessLateUpdate)
                .WithFixedUpdate(ProcessFixedUpdate)
                .WithTimeScaleChanged(OnTimeScaleChanged)
                .WithTimeComponentsOf(gameObject);
        }

        protected override IBackgroundTask OnDiscard() {
            if (Target is { HasBeenDiscarded: false }) {
                // We started to be discarded, so we need to discard Target,
                // but discarding Target will call Discard on us, so we need to remove removeAutomatically listener first
                World.EventSystem.RemoveListenersForEventOwnedBy(this, Model.Events.BeingDiscarded, true);
                Target.Discard();
            }
            return base.OnDiscard();
        }

        void OnDestroy() {
            OnGameObjectDestroy();
            if (Target is { HasBeenDiscarded: false }) {
                Target.Discard();
            }
        }

        protected virtual void ProcessUpdate(float deltaTime) {}
        protected virtual void ProcessLateUpdate(float deltaTime) { }
        protected virtual void ProcessFixedUpdate(float deltaTime) { }
        protected virtual void OnTimeScaleChanged(float from, float to) { }
        protected virtual void OnGameObjectDestroy() {}
    }
    
    public class TimeBehaviour : TimeBehaviour<TimeModel> { }
}
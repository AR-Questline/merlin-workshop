using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Timing.ARTime.Modifiers {
    public abstract partial class GradualTimeModifier : Element<TimeDependent>, ITimeModifier {
        float _delay;
        
        protected float Weight { get; private set; }

        public abstract int Order { get; }
        public string SourceID { get; }
        
        protected GradualTimeModifier(string sourceID, float delay) {
            _delay = delay;
            SourceID = sourceID;
        }

        public abstract float Modify(float timeScale);

        public void Apply() {
            AsyncApply().Forget();
        }

        public async UniTaskVoid AsyncApply() {
            if (_delay > 0) {
                float start = Time.unscaledTime;
                float elapsed = 0;
                
                do {
                    Weight = elapsed / _delay;
                    ParentModel.RefreshTimeScale();
                    if (!await AsyncUtil.DelayFrame(this)) {
                        return;
                    }
                    elapsed = Time.unscaledTime - start;
                } while (elapsed < _delay);
            }

            Weight = 1;
            ParentModel.RefreshTimeScale();
        }

        public void Remove() {
            AsyncRemove().Forget();
        }

        public async UniTaskVoid AsyncRemove() {
            if (_delay > 0) {
                float start = Time.unscaledTime;
                float elapsed = 0;
            
                do {
                    Weight = 1 - elapsed / _delay;
                    ParentModel.RefreshTimeScale();
                    if (!await AsyncUtil.DelayFrame(this)) {
                        return;
                    }
                    elapsed = Time.unscaledTime - start;
                } while (elapsed < _delay);
            }
            Discard();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (ParentModel is {HasBeenDiscarded: false}) {
                ParentModel.RefreshTimeScale();
            }

            base.OnDiscard(fromDomainDrop);
        }
    }
}
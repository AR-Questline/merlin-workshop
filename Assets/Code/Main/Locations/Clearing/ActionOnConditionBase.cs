using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Clearing {
    public abstract partial class ActionOnConditionBase : Element<Location> {
        int _conditionsToFulfill;

        protected abstract int AllConditionsToFulfil { get; }
        protected abstract Condition[] Conditions { get; }
        
        protected override void OnFullyInitialized() {
            InitAfterOneFrame().Forget();
        }
        
        async UniTaskVoid InitAfterOneFrame() {
            if (await AsyncUtil.DelayFrame(this)) {
                _conditionsToFulfill = AllConditionsToFulfil;
                if (_conditionsToFulfill == 0) {
                    AllConditionsFulfilled();
                } else {
                    SetupConditions();
                }
            }
        }

        void SetupConditions() {
            for (int i = 0; i < AllConditionsToFulfil; i++) {
                var condition = Conditions[i];
                condition.CreateAndSetup(this);
            }
        }

        public void ConditionFulfilled() {
            if (--_conditionsToFulfill == 0) {
                AllConditionsFulfilled();
            }
        }

        void AllConditionsFulfilled() {
            OnAllConditionsFulfilled();
            Discard();
        }

        protected abstract void OnAllConditionsFulfilled();
    }
}
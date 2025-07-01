using System;

namespace Awaken.TG.Main.Locations.Clearing {
    [Serializable]
    public abstract class Condition {
        public bool IsFulfilled { get; private set; }
        protected ActionOnConditionBase Owner { get; private set; }

        public void CreateAndSetup(ActionOnConditionBase owner) {
            Owner = owner;
            Setup();
        }

        protected abstract void Setup();

        protected void Fulfill() {
            if (IsFulfilled) {
                return;
            }

            IsFulfilled = true;
            Owner.ConditionFulfilled();
        }
    }
}

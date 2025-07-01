using Awaken.TG.MVC.UI;

namespace Awaken.TG.MVC {
    public abstract class RetargetableView : View {
        bool _wasInitialized = false;
        
        public IWithRecyclableView RecyclableTarget { get; private set; }

        public void ReTarget(IWithRecyclableView withRecyclableTarget) {
            if (GenericTarget != null) {
                OnOldTargetRemove();
                World.EventSystem.RemoveAllListenersOwnedBy(this, true);
            }
            ReleaseReleasable();
            
            RecyclableTarget = withRecyclableTarget;
            Initialize(Services, withRecyclableTarget);
        }

        protected override void OnInitialize() {
            if (!_wasInitialized) {
                OnFirstInit();
            }
            _wasInitialized = true;
            OnNewTarget();
        }
        
        /// <summary>
        /// Called during view initialization, after the references to
        /// world, services and target model are all set.
        /// Called only once after creation.
        /// </summary>
        protected virtual void OnFirstInit() {
            // empty, room for expansion
        }
        
        /// <summary>
        /// Called during view initialization, after the references to
        /// world, services and target model are all set.
        /// Called every time view target changes.
        /// </summary>
        protected virtual void OnOldTargetRemove() {
            // empty, room for expansion
        }

        /// <summary>
        /// Called during view initialization, after the references to
        /// world, services and target model are all set.
        /// Called every time view target changes.
        /// </summary>
        protected virtual void OnNewTarget() {
            // empty, room for expansion
        }
    }
    
    public class RetargetableView<T> : RetargetableView where T : IModel {
        public T Target => (T)GenericTarget;
        protected override bool CanNestInside(View view) => view is RetargetableView<T>;
    }
}

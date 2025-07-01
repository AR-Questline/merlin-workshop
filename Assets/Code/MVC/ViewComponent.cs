using Awaken.TG.MVC.Events;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.MVC {
    public abstract class ViewComponent : MonoBehaviour, IListenerOwner {
        public string LocID {
            get {
                if (string.IsNullOrWhiteSpace(locID)) {
                    locID = "View";
                }
                return locID;
            }
            set => locID = value;
        }
        
        [SerializeField]
        [HideInInspector]
        string locID;
        
        // === Properties

        public Services Services => World.Services;
        public bool HasBeenDiscarded => GenericTarget == null || GenericTarget.HasBeenDiscarded;
        public IModel GenericTarget { get; private set; }
        public View ParentView { get; private set; }
        [ShowInInspector, ReadOnly] bool _isAttached = false;

        // === Initialization

        public void Attach(Services services, IModel model, View view) {
            if (_isAttached) return;
            // stores stuff
            GenericTarget = model;
            ParentView = view;
            // invoke type-specific initialization
            World.EventSystem.ModalListenTo(
                EventSystem.PatternForModel(GenericTarget), 
                Model.Events.BeforeDiscarded, this,
                Discard
                );
            OnAttach();
            _isAttached = true;
        }

        void Discard(Model _) {
            if (GenericTarget == null) return;
            OnDiscard();
            World.EventSystem.RemoveAllListenersOwnedBy(this, true);
            GenericTarget = null;
        }

        protected virtual void OnAttach() { }

        protected virtual void OnDiscard() { }

        protected virtual void OnDestroy() {
            Discard(null);
        }
    }

    public abstract class ViewComponent<TModel> : ViewComponent where TModel : IModel {
        public TModel Target => (TModel) GenericTarget;
    }
}

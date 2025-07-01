using System;
using System.Text;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.MVC.Elements {
    public abstract partial class Element : Model, IElement {
        /// <summary>
        /// Null after discard
        /// </summary>
        public IModel GenericParentModel { get; private set; }
        public sealed override Domain DefaultDomain {
            get {
                var mainDomain = GenericParentModel.CurrentDomain;
                return mainDomain != default ? mainDomain : GenericParentModel.DefaultDomain;
            }
        }

        public bool IsFullyInitializedWithParents() {
            IElement wanderingElement = this;
            while (true) {
                if (!wanderingElement.IsFullyInitialized) return false;

                if (wanderingElement.GenericParentModel is IElement element) {
                    wanderingElement = element;
                    
                } else if (wanderingElement.GenericParentModel == null) {
                    return false;
                } else {
                    return wanderingElement.GenericParentModel.IsFullyInitialized;
                }
            }
        }

        /// <summary>
        /// Binds the model to a parent. Do not call this directly, use
        /// Model.AddElement to add elements instead.
        /// </summary>
        /// <param name="parent"></param>
        public void Bind(Model parent) {
            GenericParentModel = parent;
            ModelBound();
        }

        protected sealed override string GenerateID(Services services, StringBuilder idBuilder) {
            idBuilder.Append(GenericParentModel.ID);
            idBuilder.Append(':');
            AppendJustThisModelID(services, idBuilder);
            return idBuilder.ToString();
        }

        protected override void OnBeforeDiscard() {
            GenericParentModel.NotifyElementDiscarded(this);
        }

        protected override void OnFullyDiscarded() {
            GenericParentModel = null;
        }

        protected virtual void ModelBound() {}
    }

    /// <summary>
    /// Elements are special models that can be bound to another model. Attached elements
    /// automatically share their parent's lifecycle - they are added and discarded from the world
    /// together with the parent. 
    /// </summary>
    public abstract partial class Element<TParent> : Element, IElement<TParent> where TParent : IModel {
        TParent _castedParentModel;

        /// <summary>
        /// The parent model of this element. Null after discard
        /// </summary>
        public TParent ParentModel => _castedParentModel;

        protected sealed override void ModelBound() {
            _castedParentModel = (TParent)GenericParentModel;
        }

        protected sealed override void OnFullyDiscarded() {
            _castedParentModel = default;
            base.OnFullyDiscarded();
        }
    }
}

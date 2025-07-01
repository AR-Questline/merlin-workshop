using System;
using Awaken.TG.Debugging.ModelsDebugs.Inspectors;
using Awaken.TG.MVC.Utils;

namespace Awaken.TG.MVC.Relations {
    /// <summary>
    /// Used as a property in a model, allows accessing a relation to a single other model,
    /// for example to the 'parent' of this model, if the relation is 'child -> parent'.
    /// </summary>
    /// <typeparam name="TRelated">the type of the related model</typeparam>
    public class RelatedValue<TRelated> : IRelatedValue where TRelated : class, IModel {
        // === Fields

        readonly IModel _owner;
        readonly Relation<TRelated> _rel;

        // === Constructors

        public RelatedValue(Model owner, Relation<TRelated> rel) {
            _owner = owner;
            _rel = rel;
            if (_rel.OtherArity != Arity.One) {
                throw new ArgumentException(
                    "This relation has N objects on the other side, cannot use a simple Related (use RelatedList instead).");
            }
        }

        // === Operation

        public IModel Related => Get();
        
        public TRelated Get() {
            RelationStore store = _owner.GetRelationStore();
            return (TRelated)store?.SingleRelatedBy(_rel);
        }

        public void ChangeTo(TRelated newModel) {
            // attaching nothing is the same as removing the old value
            if (newModel == null) {
                Detach();
                return;
            }

            RelationStore.EstablishRelation(_rel, _rel.GenericOpposite, _owner, newModel, 0);
        }

        public void Detach() {
            RelationStore store = _owner.GetRelationStore();
            var attached = store?.SingleRelatedBy(_rel);
            if (attached != null) {
                RelationStore.BreakRelation(_rel, _rel.GenericOpposite, _owner, attached);
            }
        }

        [UnityEngine.Scripting.Preserve] public bool Exists() => Get() != null;
    }
}

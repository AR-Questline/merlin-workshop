using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC.Utils;

namespace Awaken.TG.MVC.Relations {
    /// <summary>
    /// Used as a property in a model, allows accessing a relation to multiple models
    /// for example to all the 'children' of this model, if the relation is 'parent -> children'.
    /// Manipulating this list manipulates the relation, automatically updating the inverse
    /// 'child -> parent' in the children.
    /// </summary>
    /// <typeparam name="TRelated">the type of the related models on the other side of the relation</typeparam>
    public class RelatedList<TRelated> : IEnumerable<TRelated> where TRelated : class, IModel {
        // === Fields

        IModel _owner;
        Relation<TRelated> _rel;

        // === Constructors

        public RelatedList(IModel owner, Relation<TRelated> rel) {
            _owner = owner;
            _rel = rel;
            if (_rel.OtherArity != Arity.Many) {
                throw new ArgumentException(
                    "This relation has 1 object on the other side, cannot use a RelatedList (use a simple Related instead).");
            }
        }

        // === Full IList implementation

        IEnumerator<TRelated> EmptyEnumerator => Enumerable.Empty<TRelated>().GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() {
            RelationStore store = _owner.GetRelationStore();
            if (store == null) return EmptyEnumerator;
            var list = store.AllRelatedBy(_rel);
            if (list == null) return EmptyEnumerator;
            return list.GetEnumerator();
        }

        public IEnumerator<TRelated> GetEnumerator() {
            RelationStore store = _owner.GetRelationStore();
            if (store == null) return EmptyEnumerator;
            var list = store.AllRelatedBy(_rel);
            if (list == null) return EmptyEnumerator;
            return list.Cast<TRelated>().GetEnumerator();
        }

        public bool Add(TRelated item) {
            return RelationStore.EstablishRelation(_rel, _rel.GenericOpposite, _owner, item);
        }

        public void AddRange(IEnumerable<TRelated> items) {
            foreach (TRelated item in items) {
                Add(item);
            }
        }

        public void Clear() {
            RelationStore store = _owner.GetRelationStore();
            if (store == null) return;
            var list = store.AllRelatedBy(_rel);
            while (list.Count > 0) {
                RelationStore.BreakRelation(_rel, _rel.GenericOpposite, _owner, list[0]);
            }
        }
        
        public bool Contains(TRelated item) {
            RelationStore store = _owner.GetRelationStore();
            if (store == null) return false;
            return store.ContainsRelation(_rel, item);
        }

        public void CopyTo(TRelated[] array, int arrayIndex) {
            RelationStore store = _owner.GetRelationStore();
            var list = store?.AllRelatedBy(_rel);
            list?.Cast<TRelated>().ToList().CopyTo(array, arrayIndex);
        }

        public bool Remove(TRelated item) {
            RelationStore store = _owner.GetRelationStore();
            if (store == null) return false;

            RelationStore.BreakRelation(_rel, _rel.GenericOpposite, _owner, item);
            return true;
        }

        public int Count {
            get {
                RelationStore store = _owner.GetRelationStore();
                if (store == null) return 0;
                return store.AllRelatedBy(_rel)?.Count ?? 0;
            }
        }

        public bool IsReadOnly => false;

        public int IndexOf(TRelated item) {
            RelationStore store = _owner.GetRelationStore();
            return store?.AllRelatedBy(_rel)?.IndexOf(item) ?? -1;
        }

        public void Insert(int index, TRelated item) {
            RelationStore.EstablishRelation(_rel, _rel.GenericOpposite, _owner, item, index);
        }

        [UnityEngine.Scripting.Preserve]
        public void RemoveAt(int index) {
            TRelated element = this[index];
            Remove(element);
        }

        public TRelated this[int index] {
            get {
                RelationStore store = _owner.GetRelationStore();
                if (store == null) throw new ArgumentOutOfRangeException(nameof(index));
                var list = store.AllRelatedBy(_rel);
                if (list == null) throw new ArgumentOutOfRangeException(nameof(index));
                return (TRelated)list[index];
            }
            set {
                TRelated previous = this[index];
                
                RelationStore.BreakRelation(_rel, _rel.GenericOpposite, _owner, previous);
                RelationStore.EstablishRelation(_rel, _rel.GenericOpposite, _owner, value, index);
            }
        }
    }
}

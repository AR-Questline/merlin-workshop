using System;
using System.Linq;

namespace Awaken.TG.MVC.Relations {

    /// <summary>
    /// Represents a pair of opposite relations. When one relation in a pair is established between A and B,
    /// the opposite relation will also be established between B and A.
    /// </summary>
    public abstract class RelationPair 
    {
        // === Properties

        internal Type DeclaringType { get; set; }
        protected Relation GenericLTR { get; set; }
        protected Relation GenericRTL { get; set; }
        public bool IsSaved { get; protected set; }
        public int Order { get; protected set; }
       
        // === Operations

        internal Relation GetOpposite(Relation rel) {
            if (rel == GenericLTR) return GenericRTL;
            if (rel == GenericRTL) return GenericLTR;
            throw new ArgumentException($"Relation '{rel}' is not part of this pair.");
        }

        public override string ToString() => $"RelationPair|{GenericLTR}|{GenericRTL}";
    }

    /// <inheritdoc />
    public class RelationPair<TLeft, TRight> : RelationPair
        where TLeft : class, IModel
        where TRight : class, IModel 
    {    
        // === Properties

        public Relation<TLeft, TRight> LeftToRight => (Relation<TLeft, TRight>) GenericLTR;
        public Relation<TRight, TLeft> RightToLeft => (Relation<TRight, TLeft>) GenericRTL;

        // === Constructors

        public RelationPair(Type declaringType, Arity leftArity, string leftToRight, Arity rightArity, string rightToLeft, bool isSaved = true) {
            DeclaringType = declaringType;
            GenericLTR = new Relation<TLeft, TRight>(this, leftToRight, leftArity, rightArity);
            GenericRTL = new Relation<TRight, TLeft>(this, rightToLeft, rightArity, leftArity);
            Order = 0;
            IsSaved = isSaved;
        }
        
        public RelationPair(Type declaringType, Arity leftArity, string leftToRight, Arity rightArity, string rightToLeft, RelationPair parent) 
            : this(declaringType, leftArity, leftToRight, rightArity, rightToLeft) {
            Order = parent.Order - 1;
        }
    }
}
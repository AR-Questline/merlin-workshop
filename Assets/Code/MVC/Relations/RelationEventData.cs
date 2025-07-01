namespace Awaken.TG.MVC.Relations {
    /// <summary>
    /// Payload for relation events.
    /// </summary>
    public struct RelationEventData {
        /// <summary>
        /// The left side of the relation.
        /// </summary>
        public IModel from;
        /// <summary>
        /// The right side of the relation.
        /// </summary>
        public IModel to;
        /// <summary>
        /// The relation in question.
        /// </summary>
        [UnityEngine.Scripting.Preserve] public Relation relation;
        /// <summary>
        /// If true, the relation was newly established between the two objects.
        /// If false, the relation was just broken between the two objects.
        /// </summary>
        public bool newState;

        public RelationEventData(IModel from, IModel to, Relation relation, bool newState) {
            this.from = from;
            this.to = to;
            this.relation = relation;
            this.newState = newState;
        }
    }
}
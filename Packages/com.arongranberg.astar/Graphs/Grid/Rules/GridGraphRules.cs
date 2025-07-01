using System.Collections.Generic;

namespace Pathfinding.Graphs.Grid.Rules {
	using Pathfinding.Serialization;
	using Pathfinding.Jobs;
	using Unity.Jobs;
	using Unity.Collections;
	using Unity.Mathematics;

	public class CustomGridGraphRuleEditorAttribute : System.Attribute {
		public System.Type type;
		public string name;
		public CustomGridGraphRuleEditorAttribute(System.Type type, string name) {
        }
    }

	/// <summary>
	/// Container for all rules in a grid graph.
	///
	/// <code>
	/// // Get the first grid graph in the scene
	/// var gridGraph = AstarPath.active.data.gridGraph;
	///
	/// gridGraph.rules.AddRule(new Pathfinding.Graphs.Grid.Rules.RuleAnglePenalty {
	///     penaltyScale = 10000,
	///     curve = AnimationCurve.Linear(0, 0, 90, 1),
	/// });
	/// </code>
	///
	/// See: <see cref="Pathfinding.GridGraph.rules"/>
	/// See: grid-rules (view in online documentation for working links)
	/// </summary>
	[JsonOptIn]
	public class GridGraphRules {
		List<System.Action<Context> >[] jobSystemCallbacks;
		List<System.Action<Context> >[] mainThreadCallbacks;

		/// <summary>List of all rules</summary>
		[JsonMember]
		List<GridGraphRule> rules = new List<GridGraphRule>();

		long lastHash;

		/// <summary>Context for when scanning or updating a graph</summary>
		public class Context {
			/// <summary>Graph which is being scanned or updated</summary>
			public GridGraph graph;
			/// <summary>Data for all the nodes as NativeArrays</summary>
			public GridGraphScanData data;
			/// <summary>
			/// Tracks dependencies between jobs to allow parallelism without tediously specifying dependencies manually.
			/// Always use when scheduling jobs.
			/// </summary>
			public JobDependencyTracker tracker => data.dependencyTracker;
		}

		public void AddRule (GridGraphRule rule) {
        }

        public void RemoveRule (GridGraphRule rule) {
        }

        public IReadOnlyList<GridGraphRule> GetRules () {
            return default;
        }

        long Hash () {
            return default;
        }

        public void RebuildIfNecessary () {
        }

        public void Rebuild () {
        }

        public void DisposeUnmanagedData () {
        }

        static void CallActions (List<System.Action<Context> > actions, Context context) {
        }

        /// <summary>
        /// Executes the rules for the given pass.
        /// Call handle.Complete on, or wait for, all yielded job handles.
        /// </summary>
        public IEnumerator<JobHandle> ExecuteRule (GridGraphRule.Pass rule, Context context) {
            return default;
        }

        public void ExecuteRuleMainThread (GridGraphRule.Pass rule, Context context) {
        }

        /// <summary>
        /// Adds a pass callback that uses the job system.
        /// This rule should only schedule jobs using the `Context.tracker` dependency tracker. Data is not safe to access directly in the callback
        ///
        /// This method should only be called from rules in their Register method.
        /// </summary>
        public void AddJobSystemPass(GridGraphRule.Pass pass, System.Action<Context> action)
        {
        }

        /// <summary>
        /// Adds a pass callback that runs in the main thread.
        /// The callback may access and modify any data in the context.
        /// You do not need to schedule jobs in order to access the data.
        ///
        /// Warning: Not all data in the Context is valid for every pass. For example you cannot access node connections in the BeforeConnections pass
        /// since they haven't been calculated yet.
        ///
        /// This is a bit slower than <see cref="AddJobSystemPass"/> since parallelism and the burst compiler cannot be used.
        /// But if you need to use non-thread-safe APIs or data then this is a good choice.
        ///
        /// This method should only be called from rules in their Register method.
        /// </summary>
        public void AddMainThreadPass(GridGraphRule.Pass pass, System.Action<Context> action)
        {
        }

        /// <summary>Deprecated: Use AddJobSystemPass or AddMainThreadPass instead</summary>
        [System.Obsolete("Use AddJobSystemPass or AddMainThreadPass instead")]
        public void Add(GridGraphRule.Pass pass, System.Action<Context> action)
        {
        }
    }

    /// <summary>
    /// Custom rule for a grid graph.
    /// See: <see cref="GridGraphRules"/>
    /// See: grid-rules (view in online documentation for working links)
    /// </summary>
    [JsonDynamicType]
    // Compatibility with old versions
    [JsonDynamicTypeAlias("Pathfinding.RuleTexture", typeof(RuleTexture))]
    [JsonDynamicTypeAlias("Pathfinding.RuleAnglePenalty", typeof(RuleAnglePenalty))]
    [JsonDynamicTypeAlias("Pathfinding.RuleElevationPenalty", typeof(RuleElevationPenalty))]
    [JsonDynamicTypeAlias("Pathfinding.RulePerLayerModifications", typeof(RulePerLayerModifications))]
    public abstract class GridGraphRule
    {
        /// <summary>Only enabled rules are executed</summary>
        [JsonMember]
        public bool enabled = true;
        int dirty = 1;

        /// <summary>
        /// Where in the scanning process a rule will be executed.
        /// Check the documentation for <see cref="GridGraphScanData"/> to see which data fields are valid in which passes.
        /// </summary>
        public enum Pass
        {
            /// <summary>
            /// Before the collision testing phase but after height testing.
            /// This is very early. Most data is not valid by this point.
            ///
            /// You can use this if you want to modify the node positions and still have it picked up by the collision testing code.
            /// </summary>
            BeforeCollision,
            /// <summary>
            /// Before connections are calculated.
            /// At this point height testing and collision testing has been done (if they are enabled).
            ///
            /// This is the most common pass to use.
            /// If you are modifying walkability here then connections and erosion will be calculated properly.
            /// </summary>
            BeforeConnections,
            /// <summary>
            /// After connections are calculated.
            ///
            /// If you are modifying connections directly you should do that in this pass.
            ///
            /// Note: If erosion is used then this pass will be executed twice. One time before erosion and one time after erosion
            /// when the connections are calculated again.
            /// </summary>
            AfterConnections,
            /// <summary>
            /// After erosion is calculated but before connections have been recalculated.
            ///
            /// If no erosion is used then this pass will not be executed.
            /// </summary>
            AfterErosion,
            /// <summary>
            /// After everything else.
            /// This pass is executed after everything else is done.
            /// You should not modify walkability in this pass because then the node connections will not be up to date.
            /// </summary>
            PostProcess,
            /// <summary>
            /// After the graph update has been applied to the graph.
            ///
            /// This pass can only be added as a main-thread pass.
            /// If many updates are applied to the graph at the same time, this pass will only execute once after all updates have been applied.
            ///
            /// Warning: No data in the context except the reference to the graph is valid at this point. It has all been disposed.
            /// You cannot modify any data in this pass.
            /// </summary>
            AfterApplied,
        }

        /// <summary>
        /// Hash of the settings for this rule.
        /// The <see cref="Register"/> method will be called again whenever the hash changes.
        /// If the hash does not change it is assumed that the <see cref="Register"/> method does not need to be called again.
        /// </summary>
        public virtual int Hash => dirty;

        /// <summary>
        /// Call if you have changed any setting of the rule.
        /// This will ensure that any cached data the rule uses is rebuilt.
        /// If you do not do this then any settings changes may not affect the graph when it is rescanned or updated.
        ///
        /// The purpose of this method call is to cause the <see cref="Hash"/> property to change. If your custom rule overrides the Hash property to
        /// return a hash of some settings, then you do not need to call this method for the changes the hash function already accounts for.
        /// </summary>
        public virtual void SetDirty()
        {
        }

        /// <summary>
        /// Called when the rule is removed or the graph is destroyed.
        /// Use this to e.g. clean up any NativeArrays that the rule uses.
        ///
        /// Note: The rule should remain valid after this method has been called.
        /// However the <see cref="Register"/> method is guaranteed to be called before the rule is executed again.
        /// </summary>
        public virtual void DisposeUnmanagedData()
        {
        }

        /// <summary>Does preprocessing and adds callbacks to the <see cref="GridGraphRules"/> object</summary>
        public virtual void Register(GridGraphRules rules)
        {
        }
    }
}

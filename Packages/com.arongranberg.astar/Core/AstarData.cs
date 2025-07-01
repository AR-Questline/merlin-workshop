using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Pathfinding.WindowsStore;
using Pathfinding.Serialization;
using Pathfinding.Util;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine.SceneManagement;
#if UNITY_WINRT && !UNITY_EDITOR
//using MarkerMetro.Unity.WinLegacy.IO;
//using MarkerMetro.Unity.WinLegacy.Reflection;
#endif

namespace Pathfinding {
    [System.Serializable]
    /// <summary>
    /// Stores the navigation graphs for the A* Pathfinding System.
    ///
    /// An instance of this class is assigned to <see cref="AstarPath.data"/>. From it you can access all graphs loaded through the <see cref="graphs"/> variable.
    /// This class also handles a lot of the high level serialization.
    /// </summary>
    public class AstarData {
        
#if ASTAR_FAST_NO_EXCEPTIONS || UNITY_WINRT || UNITY_WEBGL
        /// <summary>
        /// Graph types to use when building with Fast But No Exceptions for iPhone.
        /// If you add any custom graph types, you need to add them to this hard-coded list.
        /// </summary>
        public static readonly System.Type[] DefaultGraphTypes = new System.Type[] {
#if !ASTAR_NO_GRID_GRAPH
            typeof(GridGraph),
            typeof(LayerGridGraph),
#endif
#if !ASTAR_NO_POINT_GRAPH
            typeof(PointGraph),
#endif
            typeof(NavMeshGraph),
            typeof(RecastGraph),
            typeof(LinkGraph),
        };
#endif
        /// <summary>
        /// All supported graph types.
        /// Populated through reflection search
        /// </summary>
#if !ASTAR_FAST_NO_EXCEPTIONS && !UNITY_WINRT && !UNITY_WEBGL
        public static readonly Type[] graphTypes = AssemblySearcher.FindTypesInheritingFrom<NavGraph>().ToArray();
#else
        public static readonly Type[] graphTypes = DefaultGraphTypes;
#endif
        public static string PathfindingCacheDirectoryPath => System.IO.Path.Combine(Application.streamingAssetsPath, "PathfindingCache");

        /// <summary>The AstarPath component which owns this AstarData</summary>
        AstarPath active;

        #region Fields

        /// <summary>
        /// Shortcut to the first <see cref="NavMeshGraph"/>
        ///
        /// Deprecated: Use <see cref="navmeshGraph"/> instead
        /// </summary>
        [System.Obsolete("Use navmeshGraph instead")]
        public NavMeshGraph navmesh => navmeshGraph;

        /// <summary>Shortcut to the first <see cref="NavMeshGraph"/></summary>
        public NavMeshGraph navmeshGraph { get; private set; }

#if !ASTAR_NO_GRID_GRAPH
        /// <summary>Shortcut to the first <see cref="GridGraph"/></summary>
        public GridGraph gridGraph { get; private set; }

        /// <summary>Shortcut to the first <see cref="LayerGridGraph"/>.</summary>
        public LayerGridGraph layerGridGraph { get; private set; }
#endif

#if !ASTAR_NO_POINT_GRAPH
        /// <summary>Shortcut to the first <see cref="PointGraph"/>.</summary>
        public PointGraph pointGraph { get; private set; }
#endif

        /// <summary>Shortcut to the first <see cref="RecastGraph"/>.</summary>
        public RecastGraph recastGraph { get; private set; }

        /// <summary>Shortcut to the first <see cref="LinkGraph"/>.</summary>
        public LinkGraph linkGraph { get; private set; }

#if ASTAR_FAST_NO_EXCEPTIONS || UNITY_WINRT || UNITY_WEBGL
		/// <summary>
		/// Graph types to use when building with Fast But No Exceptions for iPhone.
		/// If you add any custom graph types, you need to add them to this hard-coded list.
		/// </summary>
		public static readonly System.Type[] DefaultGraphTypes = new System.Type[] {
#if !ASTAR_NO_GRID_GRAPH
			typeof(GridGraph),
			typeof(LayerGridGraph),
#endif
#if !ASTAR_NO_POINT_GRAPH
			typeof(PointGraph),
#endif
			typeof(NavMeshGraph),
			typeof(RecastGraph),
			typeof(LinkGraph),
		};
#endif

        /// <summary>
        /// All graphs.
        /// This will be filled only after deserialization has completed.
        /// May contain null entries if graph have been removed.
        /// </summary>
        [System.NonSerialized]
        public NavGraph[] graphs = new NavGraph[0];

        /// <summary>
        /// Serialized data for all graphs and settings.
        /// Stored as a base64 encoded string because otherwise Unity's Undo system would sometimes corrupt the byte data (because it only stores deltas).
        ///
        /// This can be accessed as a byte array from the <see cref="data"/> property.
        /// </summary>
        [SerializeField]
        string dataString;

        /// <summary>Serialized data for all graphs and settings</summary>
        private byte[] data {
            get {
                var d = dataString != null ? System.Convert.FromBase64String(dataString) : null;
                // Unity can initialize the dataString to an empty string, but that's not a valid zip file
                if (d != null && d.Length == 0) return null;
                return d;
            }
            set { dataString = value != null ? System.Convert.ToBase64String(value) : null; }
        }

        /// <summary>
        /// Should graph-data be cached.
        /// Caching the startup means saving the whole graphs - not only the settings - to a file (<see cref="file_cachedStartup)"/> which can
        /// be loaded when the game starts. This is usually much faster than scanning the graphs when the game starts. This is configured from the editor under the "Save & Load" tab.
        ///
        /// [Open online documentation to see images]
        ///
        /// See: save-load-graphs (view in online documentation for working links)
        /// </summary>
        [SerializeField]
        public bool cacheStartup;

        List<bool> graphStructureLocked = new List<bool>();

        static readonly Unity.Profiling.ProfilerMarker MarkerLoadFromCache = new Unity.Profiling.ProfilerMarker("LoadFromCache");
        static readonly Unity.Profiling.ProfilerMarker MarkerDeserializeGraphs = new Unity.Profiling.ProfilerMarker("DeserializeGraphs");
        static readonly Unity.Profiling.ProfilerMarker MarkerSerializeGraphs = new Unity.Profiling.ProfilerMarker("SerializeGraphs");

        #endregion

        internal AstarData(AstarPath active) {
        }

        public string cacheFileName {
            get {
                if (active == null) {
                    Debug.LogError("Using invalid AstarData");
                    return string.Empty;
                }

                var sceneName = active.gameObject.scene.name;
                if (sceneName.EndsWith("_Static")) {
                    sceneName = sceneName.Remove(sceneName.Length - 7, 7);
                }

                return sceneName + ".bytes";
            }
        }

        public string cacheFilePath => System.IO.Path.Combine(PathfindingCacheDirectoryPath, cacheFileName);
        public bool hasCacheFile => File.Exists(cacheFilePath);

        /// <summary>Get the serialized data for all graphs and their settings</summary>
        public byte[] GetData() => data;

        /// <summary>
        /// Set the serialized data for all graphs and their settings.
        ///
        /// During runtime you usually want to deserialize the graphs immediately, in which case you should use <see cref="DeserializeGraphs(byte"/>[]) instead.
        /// </summary>
        public void SetData(byte[] data) {
        }

        /// <summary>Loads the graphs from memory, will load cached graphs if any exists</summary>
        public void OnEnable() {
        }

        /// <summary>
        /// Prevent the graph structure from changing during the time this lock is held.
        /// This prevents graphs from being added or removed and also prevents graphs from being serialized or deserialized.
        /// This is used when e.g an async scan is happening to ensure that for example a graph that is being scanned is not destroyed.
        ///
        /// Each call to this method *must* be paired with exactly one call to <see cref="UnlockGraphStructure"/>.
        /// The calls may be nested.
        /// </summary>
        internal void LockGraphStructure(bool allowAddingGraphs = false) {
        }

        /// <summary>
        /// Allows the graph structure to change again.
        /// See: <see cref="LockGraphStructure"/>
        /// </summary>
        internal void UnlockGraphStructure() {
        }

        PathProcessor.GraphUpdateLock AssertSafe(bool onlyAddingGraph = false) {
            return default;
        }

        /// <summary>
        /// Calls the callback with every node in all graphs.
        /// This is the easiest way to iterate through every existing node.
        ///
        /// <code>
        /// AstarPath.active.data.GetNodes(node => {
        ///     Debug.Log("I found a node at position " + (Vector3)node.position);
        /// });
        /// </code>
        ///
        /// See: <see cref="Pathfinding.NavGraph.GetNodes"/> for getting the nodes of a single graph instead of all.
        /// See: graph-updates (view in online documentation for working links)
        /// </summary>
        public void GetNodes(System.Action<GraphNode> callback) {
        }

        /// <summary>
        /// Updates shortcuts to the first graph of different types.
        /// Hard coding references to some graph types is not really a good thing imo. I want to keep it dynamic and flexible.
        /// But these references ease the use of the system, so I decided to keep them.
        /// </summary>
        public void UpdateShortcuts() {
        }

        /// <summary>Load from data from <see cref="file_cachedStartup"/></summary>
        public void LoadFromCache() {
        }

        void TryLoadGraphsFromCache() {
        }

        #region Serialization

        static unsafe NativeArray<byte> ReadByteArray(string filepath, long count, Allocator allocator) {
            return default;
        }

        /// <summary>
        /// Serializes all graphs settings to a byte array.
        /// See: DeserializeGraphs(byte[])
        /// </summary>
        public byte[] SerializeGraphs() {
            return default;
        }

        /// <summary>
        /// Serializes all graphs settings and optionally node data to a byte array.
        /// See: DeserializeGraphs(byte[])
        /// See: Pathfinding.Serialization.SerializeSettings
        /// </summary>
        public byte[] SerializeGraphs(SerializeSettings settings) {
            return default;
        }

        /// <summary>
        /// Main serializer function.
        /// Serializes all graphs to a byte array
        /// A similar function exists in the AstarPathEditor.cs script to save additional info
        /// </summary>
        public byte[] SerializeGraphs(SerializeSettings settings, out uint checksum) {
            checksum = default(uint);
            return default;
        }

        byte[] SerializeGraphs(SerializeSettings settings, out uint checksum, NavGraph[] graphs) {
            checksum = default(uint);
            return default;
        }

        /// <summary>Deserializes graphs from <see cref="data"/></summary>
        public void DeserializeGraphs() {
        }

        /// <summary>
        /// Destroys all graphs and sets <see cref="graphs"/> to null.
        /// See: <see cref="RemoveGraph"/>
        /// </summary>
        public void ClearGraphs() {
        }

        void ClearGraphsInternal() {
        }

        public void DisposeUnmanagedData() {
        }

        /// <summary>Makes all graphs become unscanned</summary>
        internal void DestroyAllNodes() {
        }

        public void OnDestroy() {
        }

        /// <summary>
        /// Deserializes and loads graphs from the specified byte array.
        /// An error will be logged if deserialization fails.
        ///
        /// Returns: The deserialized graphs
        /// </summary>
        public NavGraph[] DeserializeGraphs(ReadOnlySpan<byte> bytes) {
            return default;
        }

        /// <summary>
        /// Deserializes and loads graphs from the specified byte array additively.
        /// An error will be logged if deserialization fails.
        /// This function will add loaded graphs to the current ones.
        ///
        /// Returns: The deserialized graphs
        /// </summary>
        public NavGraph[] DeserializeGraphsAdditive(ReadOnlySpan<byte> bytes) {
            return default;
        }

        /// <summary>Helper function for deserializing graphs</summary>
        NavGraph[] DeserializeGraphsPartAdditive(AstarSerializer sr) {
            return default;
        }

        #endregion

        #region GraphCreation

        /// <summary>Creates a new graph instance of type type</summary>
        internal NavGraph CreateGraph(System.Type type)
        {
            return default;
        }

        /// <summary>
        /// Adds a graph of type T to the <see cref="graphs"/> array.
        /// See: runtime-graphs (view in online documentation for working links)
        /// </summary>
        public T AddGraph<T>() where T : NavGraph => AddGraph(typeof(T)) as T;

        /// <summary>
        /// Adds a graph of type type to the <see cref="graphs"/> array.
        /// See: runtime-graphs (view in online documentation for working links)
        /// </summary>
        public NavGraph AddGraph(System.Type type)
        {
            return default;
        }

        /// <summary>Adds the specified graph to the <see cref="graphs"/> array</summary>
        void AddGraph(NavGraph graph)
        {
        }

        /// <summary>
        /// Removes the specified graph from the <see cref="graphs"/> array and Destroys it in a safe manner.
        /// To avoid changing graph indices for the other graphs, the graph is simply nulled in the array instead
        /// of actually removing it from the array.
        /// The empty position will be reused if a new graph is added.
        ///
        /// Returns: True if the graph was sucessfully removed (i.e it did exist in the <see cref="graphs"/> array). False otherwise.
        ///
        /// See: <see cref="ClearGraphs"/>
        /// </summary>
        public bool RemoveGraph(NavGraph graph)
        {
            return default;
        }

        /// <summary>
        /// Duplicates the given graph and adds the duplicate to the <see cref="graphs"/> array.
        ///
        /// Note: Only graph settings are duplicated, not the nodes in the graph. You may want to scan the graph after duplicating it.
        ///
        /// Returns: The duplicated graph.
        /// </summary>
        public NavGraph DuplicateGraph(NavGraph graph)
        {
            return default;
        }

        #endregion

        #region GraphUtility

        /// <summary>
        /// Graph which contains the specified node.
        /// The graph must be in the <see cref="graphs"/> array.
        ///
        /// Returns: Returns the graph which contains the node. Null if the graph wasn't found
        /// </summary>
        public static NavGraph GetGraph(GraphNode node)
        {
            return default;
        }

        /// <summary>Returns the first graph which satisfies the predicate. Returns null if no graph was found.</summary>
        public NavGraph FindGraph(System.Func<NavGraph, bool> predicate)
        {
            return default;
        }

        /// <summary>Returns the first graph of type type found in the <see cref="graphs"/> array. Returns null if no graph was found.</summary>
        public NavGraph FindGraphOfType(System.Type type)
        {
            return default;
        }

        /// <summary>Returns the first graph which inherits from the type type. Returns null if no graph was found.</summary>
        public NavGraph FindGraphWhichInheritsFrom(System.Type type) {
            return default;
        }

        /// <summary>
        /// Loop through this function to get all graphs of type 'type'
        /// <code>
        /// foreach (GridGraph graph in AstarPath.data.FindGraphsOfType (typeof(GridGraph))) {
        ///     //Do something with the graph
        /// }
        /// </code>
        /// See: <see cref="AstarPath.AddWorkItem"/>
        /// </summary>
        public IEnumerable FindGraphsOfType(System.Type type)
        {
            return default;
        }

        /// <summary>
        /// All graphs which implements the UpdateableGraph interface
        /// <code> foreach (IUpdatableGraph graph in AstarPath.data.GetUpdateableGraphs ()) {
        ///  //Do something with the graph
        /// } </code>
        /// See: <see cref="AstarPath.AddWorkItem"/>
        /// See: <see cref="IUpdatableGraph"/>
        /// </summary>
        public IEnumerable GetUpdateableGraphs()
        {
            return default;
        }

        /// <summary>Gets the index of the graph in the <see cref="graphs"/> array</summary>
        public int GetGraphIndex(NavGraph graph)
        {
            return default;
        }

        #endregion
    }
}
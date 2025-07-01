using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using Pathfinding.Drawing;
using UnityEngine.Profiling;
using Pathfinding.Util;
using Pathfinding.Graphs.Navmesh;
using Pathfinding.Graphs.Util;
using Pathfinding.Jobs;
using Pathfinding.Collections;
using Pathfinding.Sync;
using Unity.Jobs;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

#if NETFX_CORE
using Thread = Pathfinding.WindowsStore.Thread;
#else
using Thread = System.Threading.Thread;
#endif

[ExecuteInEditMode]
[AddComponentMenu("Pathfinding/AstarPath")]
[DisallowMultipleComponent]
/// <summary>
/// Core component for the A* Pathfinding System.
/// This class handles all of the pathfinding system, calculates all paths and stores the info.
/// This class is a singleton class, meaning there should only exist at most one active instance of it in the scene.
/// It might be a bit hard to use directly, usually interfacing with the pathfinding system is done through the <see cref="Pathfinding.Seeker"/> class.
/// </summary>
[HelpURL("https://arongranberg.com/astar/documentation/stable/astarpath.html")]
public class AstarPath : VersionedMonoBehaviour {
	/// <summary>The version number for the A* Pathfinding Project</summary>
	public static readonly System.Version Version = new System.Version(5, 3, 3);

	/// <summary>Information about where the package was downloaded</summary>
	public enum AstarDistribution { WebsiteDownload, AssetStore, PackageManager };

	/// <summary>Used by the editor to guide the user to the correct place to download updates</summary>
	public static readonly AstarDistribution Distribution = AstarDistribution.AssetStore;

	/// <summary>
	/// Which branch of the A* Pathfinding Project is this release.
	/// Used when checking for updates so that users of the development
	/// versions can get notifications of development updates.
	/// </summary>
	public static readonly string Branch = "master";

	/// <summary>Holds all graph data</summary>
	[UnityEngine.Serialization.FormerlySerializedAs("astarData")]
	public AstarData data;

	/// <summary>
	/// Returns the active AstarPath object in the scene.
	/// Note: This is only set if the AstarPath object has been initialized (which happens in Awake).
	/// </summary>
	public static AstarPath active;

	/// <summary>Shortcut to <see cref="AstarData.graphs"/></summary>
	public NavGraph[] graphs => data.graphs;

	bool hasScannedGraphAtStartup = false;
	/// <summary>Holds INavmeshHolder references for all graph indices to be able to access them in a performant manner</summary>
	public INavmeshHolder[] _navmeshHolders = new INavmeshHolder[0];


	#region InspectorDebug
	/// <summary>
	/// Visualize graphs in the scene view (editor only).
	/// [Open online documentation to see images]
	/// </summary>
	public bool ShowNavGraphs {
#if UNITY_EDITOR
		get => UnityEditor.EditorPrefs.GetBool("AstarPathfindingProject.ShowGraphs", false);
		set => UnityEditor.EditorPrefs.SetBool("AstarPathfindingProject.ShowGraphs", value);
#else
		get => false;
		// ReSharper disable once ValueParameterNotUsed
		set => Debug.LogWarning("ShowNavGraphs has no effect when not in the Unity Editor");
#endif
	}
	
	static readonly int AstarAlineGraphOffsetId = Shader.PropertyToID("_AstarAlineGraphOffset");
	static float? s_navGraphsOffset = 0;

	public static float NavGraphsOffset {
		get => s_navGraphsOffset ??= Shader.GetGlobalFloat(AstarAlineGraphOffsetId);
		set {
			s_navGraphsOffset = value;
			Shader.SetGlobalFloat(AstarAlineGraphOffsetId, value);
		}
	}

	/// <summary>
	/// Toggle to show unwalkable nodes.
	///
	/// Note: Only relevant in the editor
	///
	/// See: <see cref="unwalkableNodeDebugSize"/>
	/// </summary>
	public bool showUnwalkableNodes = true;

	/// <summary>
	/// The mode to use for drawing nodes in the sceneview.
	///
	/// Note: Only relevant in the editor
	///
	/// See: <see cref="GraphDebugMode"/>
	/// </summary>
	public GraphDebugMode debugMode;

	/// <summary>
	/// Low value to use for certain <see cref="debugMode"/> modes.
	/// For example if <see cref="debugMode"/> is set to G, this value will determine when the node will be completely red.
	///
	/// Note: Only relevant in the editor
	///
	/// See: <see cref="debugRoof"/>
	/// See: <see cref="debugMode"/>
	/// </summary>
	public float debugFloor = 0;

	/// <summary>
	/// High value to use for certain <see cref="debugMode"/> modes.
	/// For example if <see cref="debugMode"/> is set to G, this value will determine when the node will be completely green.
	///
	/// For the penalty debug mode, the nodes will be colored green when they have a penalty less than <see cref="debugFloor"/> and red
	/// when their penalty is greater or equal to this value and something between red and green otherwise.
	///
	/// Note: Only relevant in the editor
	///
	/// See: <see cref="debugFloor"/>
	/// See: <see cref="debugMode"/>
	/// </summary>
	public float debugRoof = 20000;

	/// <summary>
	/// If set, the <see cref="debugFloor"/> and <see cref="debugRoof"/> values will not be automatically recalculated.
	///
	/// Note: Only relevant in the editor
	/// </summary>
	public bool manualDebugFloorRoof = false;


	/// <summary>
	/// If enabled, nodes will draw a line to their 'parent'.
	/// This will show the search tree for the latest path.
	///
	/// Note: Only relevant in the editor
	/// </summary>
	public bool showSearchTree = false;

	/// <summary>
	/// Size of the red cubes shown in place of unwalkable nodes.
	///
	/// Note: Only relevant in the editor. Does not apply to grid graphs.
	/// See: <see cref="showUnwalkableNodes"/>
	/// </summary>
	public float unwalkableNodeDebugSize = 0.3F;

	/// <summary>
	/// The amount of debugging messages.
	/// Use less debugging to improve performance (a bit) or just to get rid of the Console spamming.
	/// Use more debugging (heavy) if you want more information about what the pathfinding scripts are doing.
	/// The InGame option will display the latest path log using in-game GUI.
	///
	/// [Open online documentation to see images]
	/// </summary>
	public PathLog logPathResults = PathLog.Normal;

	#endregion

	#region InspectorSettings
	/// <summary>
	/// Maximum distance to search for nodes.
	/// When searching for the nearest node to a point, this is the limit (in world units) for how far away it is allowed to be.
	///
	/// This is relevant if you try to request a path to a point that cannot be reached and it thus has to search for
	/// the closest node to that point which can be reached (which might be far away). If it cannot find a node within this distance
	/// then the path will fail.
	///
	/// [Open online documentation to see images]
	///
	/// See: <see cref="NNConstraint.constrainDistance"/>
	/// </summary>
	public float maxNearestNodeDistance = 100;

	/// <summary>
	/// Max Nearest Node Distance Squared.
	/// See: <see cref="maxNearestNodeDistance"/>
	/// </summary>
	public float maxNearestNodeDistanceSqr => maxNearestNodeDistance*maxNearestNodeDistance;

	/// <summary>
	/// If true, all graphs will be scanned when the game starts, during OnEnable.
	/// If you disable this, you will have to call <see cref="AstarPath.active.Scan"/> yourself to enable pathfinding.
	/// Alternatively you could load a saved graph from a file.
	///
	/// If a startup cache has been generated (see save-load-graphs) (view in online documentation for working links), it always takes priority, and the graphs will be loaded from the cache instead of scanned.
	///
	/// This can be useful to disable if you want to scan your graphs asynchronously, or if you have a procedural world which has not been created yet
	/// at the start of the game.
	///
	/// See: <see cref="Scan"/>
	/// See: <see cref="ScanAsync"/>
	/// </summary>
	public bool scanOnStartup = true;

	/// <summary>
	/// Do a full GetNearest search for all graphs.
	/// Additional searches will normally only be done on the graph which in the first fast search seemed to have the closest node.
	/// With this setting on, additional searches will be done on all graphs since the first check is not always completely accurate.
	/// More technically: GetNearestForce on all graphs will be called if true, otherwise only on the one graph which's GetNearest search returned the best node.
	/// Usually faster when disabled, but higher quality searches when enabled.
	/// Note: For the PointGraph this setting doesn't matter much as it has only one search mode.
	/// </summary>
	[System.Obsolete("This setting has been removed. It is now always true", true)]
	public bool fullGetNearestSearch = false;

	/// <summary>
	/// Prioritize graphs.
	/// Graphs will be prioritized based on their order in the inspector.
	/// The first graph which has a node closer than <see cref="prioritizeGraphsLimit"/> will be chosen instead of searching all graphs.
	///
	/// Deprecated: This setting has been removed. It was always a bit of a hack. Use NNConstraint.graphMask if you want to choose which graphs are searched.
	/// </summary>
	[System.Obsolete("This setting has been removed. It was always a bit of a hack. Use NNConstraint.graphMask if you want to choose which graphs are searched.", true)]
	public bool prioritizeGraphs = false;

	/// <summary>
	/// Distance limit for <see cref="prioritizeGraphs"/>.
	/// See: <see cref="prioritizeGraphs"/>
	///
	/// Deprecated: This setting has been removed. It was always a bit of a hack. Use NNConstraint.graphMask if you want to choose which graphs are searched.
	/// </summary>
	[System.Obsolete("This setting has been removed. It was always a bit of a hack. Use NNConstraint.graphMask if you want to choose which graphs are searched.", true)]
	public float prioritizeGraphsLimit = 1F;

	/// <summary>
	/// Reference to the color settings for this AstarPath object.
	/// Color settings include for example which color the nodes should be in, in the sceneview.
	/// </summary>
	public AstarColor colorSettings;

	/// <summary>
	/// Stored tag names.
	/// See: AstarPath.FindTagNames
	/// See: AstarPath.GetTagNames
	/// </summary>
	[SerializeField]
	protected string[] tagNames = null;

	/// <summary>
	/// The distance function to use as a heuristic.
	/// The heuristic, often referred to as just 'H' is the estimated cost from a node to the target.
	/// Different heuristics affect how the path picks which one to follow from multiple possible with the same length
	/// See: <see cref="Pathfinding.Heuristic"/> for more details and descriptions of the different modes.
	/// See: <a href="https://en.wikipedia.org/wiki/Admissible_heuristic">Wikipedia: Admissible heuristic</a>
	/// See: <a href="https://en.wikipedia.org/wiki/A*_search_algorithm">Wikipedia: A* search algorithm</a>
	/// See: <a href="https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm">Wikipedia: Dijkstra's Algorithm</a>
	///
	/// Warning: Reducing the heuristic scale below 1, or disabling the heuristic, can significantly increase the cpu cost for pathfinding, especially for large graphs.
	/// </summary>
	public Heuristic heuristic = Heuristic.Euclidean;

	/// <summary>
	/// The scale of the heuristic.
	/// If a value lower than 1 is used, the pathfinder will search more nodes (slower).
	/// If 0 is used, the pathfinding algorithm will be reduced to dijkstra's algorithm. This is equivalent to setting <see cref="heuristic"/> to None.
	/// If a value larger than 1 is used the pathfinding will (usually) be faster because it expands fewer nodes, but the paths may no longer be the optimal (i.e the shortest possible paths).
	///
	/// Usually you should leave this to the default value of 1.
	///
	/// Warning: Reducing the heuristic scale below 1, or disabling the heuristic, can significantly increase the cpu cost for pathfinding, especially for large graphs.
	///
	/// See: <a href="https://en.wikipedia.org/wiki/Admissible_heuristic">Wikipedia: Admissible heuristic</a>
	/// See: <a href="https://en.wikipedia.org/wiki/A*_search_algorithm">Wikipedia: A* search algorithm</a>
	/// See: <a href="https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm">Wikipedia: Dijkstra's Algorithm</a>
	/// </summary>
	public float heuristicScale = 1F;

	/// <summary>
	/// Number of pathfinding threads to use.
	/// Multithreading puts pathfinding in another thread, this is great for performance on 2+ core computers since the framerate will barely be affected by the pathfinding at all.
	/// - None indicates that the pathfinding is run in the Unity thread as a coroutine
	/// - Automatic will try to adjust the number of threads to the number of cores and memory on the computer.
	///  Less than 512mb of memory or a single core computer will make it revert to using no multithreading.
	///
	/// It is recommended that you use one of the "Auto" settings that are available.
	/// The reason is that even if your computer might be beefy and have 8 cores.
	/// Other computers might only be quad core or dual core in which case they will not benefit from more than
	/// 1 or 3 threads respectively (you usually want to leave one core for the unity thread).
	/// If you use more threads than the number of cores on the computer it is mostly just wasting memory, it will not run any faster.
	/// The extra memory usage is not trivially small. Each thread needs to keep a small amount of data for each node in all the graphs.
	/// It is not the full graph data but it is proportional to the number of nodes.
	/// The automatic settings will inspect the machine it is running on and use that to determine the number of threads so that no memory is wasted.
	///
	/// The exception is if you only have one (or maybe two characters) active at time. Then you should probably just go with one thread always since it is very unlikely
	/// that you will need the extra throughput given by more threads. Keep in mind that more threads primarily increases throughput by calculating different paths on different
	/// threads, it will not calculate individual paths any faster.
	///
	/// Warning: If you are modifying the pathfinding core scripts or if you are directly modifying graph data without using any of the
	/// safe wrappers (like <see cref="AddWorkItem)"/>, multithreading can cause strange errors and cause pathfinding to stop working if you are not careful.
	///
	/// Note: WebGL does not support threads at all (since javascript is single-threaded) so no threads will be used on that platform.
	///
	/// Note: This setting only applies to pathfinding. Graph updates use the Unity Job System, which uses a different thread pool.
	///
	/// See: CalculateThreadCount
	/// </summary>
	public ThreadCount threadCount = ThreadCount.One;

	/// <summary>
	/// Max number of milliseconds to spend on pathfinding during each frame.
	/// At least 500 nodes will be searched each frame (if there are that many to search).
	/// When using multithreading this value is irrelevant.
	/// </summary>
	public float maxFrameTime = 1F;

	/// <summary>
	/// Throttle graph updates and batch them to improve performance.
	/// If toggled, graph updates will batched and executed less often (specified by <see cref="graphUpdateBatchingInterval)"/>.
	///
	/// This can have a positive impact on pathfinding throughput since the pathfinding threads do not need
	/// to be stopped as often, and it reduces the overhead per graph update.
	/// All graph updates are still applied, they are just batched together so that more of them are
	/// applied at the same time.
	///
	/// Do not use this if you want minimal latency between a graph update being requested
	/// and it being applied.
	///
	/// This only applies to graph updates requested using the <see cref="UpdateGraphs"/> method. Not those requested
	/// using <see cref="AddWorkItem"/>.
	///
	/// If you want to apply graph updates immediately at some point, you can call <see cref="FlushGraphUpdates"/>.
	///
	/// See: graph-updates (view in online documentation for working links)
	/// </summary>
	public bool batchGraphUpdates = false;

	/// <summary>
	/// Minimum number of seconds between each batch of graph updates.
	/// If <see cref="batchGraphUpdates"/> is true, this defines the minimum number of seconds between each batch of graph updates.
	///
	/// This can have a positive impact on pathfinding throughput since the pathfinding threads do not need
	/// to be stopped as often, and it reduces the overhead per graph update.
	/// All graph updates are still applied however, they are just batched together so that more of them are
	/// applied at the same time.
	///
	/// Do not use this if you want minimal latency between a graph update being requested
	/// and it being applied.
	///
	/// This only applies to graph updates requested using the <see cref="UpdateGraphs"/> method. Not those requested
	/// using <see cref="AddWorkItem"/>.
	///
	/// See: graph-updates (view in online documentation for working links)
	/// </summary>
	public float graphUpdateBatchingInterval = 0.2F;

	#endregion

	#region DebugVariables
#if ProfileAstar
	/// <summary>
	/// How many paths has been computed this run. From application start.
	/// Debugging variable
	/// </summary>
	public static int PathsCompleted = 0;

	public static System.Int64 TotalSearchedNodes = 0;
	public static System.Int64 TotalSearchTime = 0;
#endif

	/// <summary>The time it took for the last call to <see cref="Scan"/> to complete</summary>
	public float lastScanTime { get; private set; }

	/// <summary>
	/// The path to debug using gizmos.
	/// This is the path handler used to calculate the last path.
	/// It is used in the editor to draw debug information using gizmos.
	/// </summary>
	[System.NonSerialized]
	internal PathHandler debugPathData;

	/// <summary>The path ID to debug using gizmos</summary>
	[System.NonSerialized]
	internal ushort debugPathID;

	/// <summary>
	/// Debug string from the last completed path.
	/// Will be updated if <see cref="logPathResults"/> == PathLog.InGame
	/// </summary>
	string inGameDebugPath;

	#endregion

	#region StatusVariables

	/// <summary>
	/// True while any graphs are being scanned.
	///
	/// This is primarily relevant when scanning graph asynchronously.
	///
	/// Note: Not to be confused with graph updates.
	///
	/// Note: This will be false during <see cref="OnLatePostScan"/> and during the <see cref="GraphModifier.EventType"/>.LatePostScan event.
	///
	/// See: IsAnyGraphUpdateQueued
	/// See: IsAnyGraphUpdateInProgress
	/// </summary>
	[field: System.NonSerialized]
	public bool isScanning { get; private set; }

	/// <summary>
	/// Number of parallel pathfinders.
	/// Returns the number of concurrent processes which can calculate paths at once.
	/// When using multithreading, this will be the number of threads, if not using multithreading it is always 1 (since only 1 coroutine is used).
	/// See: IsUsingMultithreading
	/// </summary>
	public int NumParallelThreads => pathProcessor.NumThreads;

	/// <summary>
	/// Returns whether or not multithreading is used.
	/// \exception System.Exception Is thrown when it could not be decided if multithreading was used or not.
	/// This should not happen if pathfinding is set up correctly.
	/// Note: This uses info about if threads are running right now, it does not use info from the settings on the A* object.
	/// </summary>
	public bool IsUsingMultithreading => pathProcessor.IsUsingMultithreading;

	/// <summary>
	/// Returns if any graph updates are waiting to be applied.
	/// Note: This is false while the updates are being performed.
	/// Note: This does *not* includes other types of work items such as navmesh cutting or anything added by <see cref="AddWorkItem"/>.
	/// </summary>
	public bool IsAnyGraphUpdateQueued => graphUpdates.IsAnyGraphUpdateQueued;

	/// <summary>
	/// Returns if any graph updates are being calculated right now.
	/// Note: This does *not* includes other types of work items such as navmesh cutting or anything added by <see cref="AddWorkItem"/>.
	///
	/// See: IsAnyWorkItemInProgress
	/// </summary>
	public bool IsAnyGraphUpdateInProgress => graphUpdates.IsAnyGraphUpdateInProgress;

	/// <summary>
	/// Returns if any work items are in progress right now.
	/// Note: This includes pretty much all types of graph updates.
	/// Such as normal graph updates, navmesh cutting and anything added by <see cref="AddWorkItem"/>.
	/// </summary>
	public bool IsAnyWorkItemInProgress => workItems.workItemsInProgress;

	/// <summary>
	/// Returns if this code is currently being exectuted inside a work item.
	/// Note: This includes pretty much all types of graph updates.
	/// Such as normal graph updates, navmesh cutting and anything added by <see cref="AddWorkItem"/>.
	///
	/// In contrast to <see cref="IsAnyWorkItemInProgress"/> this is only true when work item code is being executed, it is not
	/// true in-between the updates to a work item that takes several frames to complete.
	/// </summary>
	internal bool IsInsideWorkItem => workItems.workItemsInProgressRightNow;

	#endregion

	#region Callbacks
	/// <summary>
	/// Called on Awake before anything else is done.
	/// This is called at the start of the Awake call, right after <see cref="active"/> has been set, but this is the only thing that has been done.
	/// Use this when you want to set up default settings for an AstarPath component created during runtime since some settings can only be changed in Awake
	/// (such as multithreading related stuff)
	/// <code>
	/// // Create a new AstarPath object on Start and apply some default settings
	/// public void Start () {
	///     AstarPath.OnAwakeSettings += ApplySettings;
	///     AstarPath astar = gameObject.AddComponent<AstarPath>();
	/// }
	///
	/// public void ApplySettings () {
	///     // Unregister from the delegate
	///     AstarPath.OnAwakeSettings -= ApplySettings;
	///     // For example threadCount should not be changed after the Awake call
	///     // so here's the only place to set it if you create the component during runtime
	///     AstarPath.active.threadCount = ThreadCount.One;
	/// }
	/// </code>
	/// </summary>
	public static System.Action OnAwakeSettings;

	/// <summary>Called for each graph before they are scanned. In most cases it is recommended to create a custom class which inherits from Pathfinding.GraphModifier instead.</summary>
	public static OnGraphDelegate OnGraphPreScan;

	/// <summary>Called for each graph after they have been scanned. All other graphs might not have been scanned yet. In most cases it is recommended to create a custom class which inherits from Pathfinding.GraphModifier instead.</summary>
	public static OnGraphDelegate OnGraphPostScan;

	/// <summary>Called for each path before searching. Be careful when using multithreading since this will be called from a different thread.</summary>
	public static OnPathDelegate OnPathPreSearch;

	/// <summary>Called for each path after searching. Be careful when using multithreading since this will be called from a different thread.</summary>
	public static OnPathDelegate OnPathPostSearch;

	/// <summary>Called before starting the scanning. In most cases it is recommended to create a custom class which inherits from Pathfinding.GraphModifier instead.</summary>
	public static OnScanDelegate OnPreScan;

	/// <summary>Called after scanning. This is called before applying links, flood-filling the graphs and other post processing. In most cases it is recommended to create a custom class which inherits from Pathfinding.GraphModifier instead.</summary>
	public static OnScanDelegate OnPostScan;

	/// <summary>Called after scanning has completed fully. This is called as the last thing in the Scan function. In most cases it is recommended to create a custom class which inherits from Pathfinding.GraphModifier instead.</summary>
	public static OnScanDelegate OnLatePostScan;

	/// <summary>Called when any graphs are updated. Register to for example recalculate the path whenever a graph changes. In most cases it is recommended to create a custom class which inherits from Pathfinding.GraphModifier instead.</summary>
	public static OnScanDelegate OnGraphsUpdated;

	/// <summary>
	/// Called when pathID overflows 65536 and resets back to zero.
	/// Note: This callback will be cleared every time it is called, so if you want to register to it repeatedly, register to it directly on receiving the callback as well.
	/// </summary>
	public static System.Action On65KOverflow;

	/// <summary>
	/// Called right after callbacks on paths have been called.
	///
	/// A path's callback function runs on the main thread when the path has been calculated.
	/// This is done in batches for all paths that have finished their calculation since the last frame.
	/// This event will trigger right after a batch of callbacks have been called.
	///
	/// If you do not want to use individual path callbacks, you can use this instead to poll all pending paths
	/// and see which ones have completed. This is better than doing it in e.g. the Update loop, because
	/// here you will have a guarantee that all calculated paths are still valid.
	/// Immediately after this callback has finished, other things may invalidate calculated paths, like for example
	/// graph updates.
	///
	/// This is used by the ECS integration to update all entities' pending paths, without having to store
	/// a callback for each agent, and also to avoid the ECS synchronization overhead that having individual
	/// callbacks would entail.
	/// </summary>
	public static System.Action OnPathsCalculated;

	#endregion

	#region MemoryStructures

	/// <summary>Processes graph updates</summary>
	readonly GraphUpdateProcessor graphUpdates;

	/// <summary>Holds a hierarchical graph to speed up some queries like if there is a path between two nodes</summary>
	internal readonly HierarchicalGraph hierarchicalGraph;

	/// <summary>Holds all active off-mesh links</summary>
	public readonly OffMeshLinks offMeshLinks;

	/// <summary>
	/// Handles navmesh cuts.
	/// See: <see cref="Pathfinding.NavmeshCut"/>
	/// </summary>
	public NavmeshUpdates navmeshUpdates = new NavmeshUpdates();

	/// <summary>Processes work items</summary>
	readonly WorkItemProcessor workItems;

	/// <summary>Holds all paths waiting to be calculated and calculates them</summary>
	readonly PathProcessor pathProcessor;

	/// <summary>Holds global node data that cannot be stored in individual graphs</summary>
	internal GlobalNodeStorage nodeStorage;

	/// <summary>
	/// Global read-write lock for graph data.
	///
	/// Graph data is always consistent from the main-thread's perspective, but if you are using jobs to read from graph data, you may need this.
	///
	/// A write lock is held automatically...
	/// - During graph updates. During async graph updates, the lock is only held once per frame while the graph update is actually running, not for the whole duration.
	/// - During work items. Async work items work similarly to graph updates, the lock is only held once per frame while the work item is actually running.
	/// - When <see cref="GraphModifier"/> events run.
	/// - When graph related callbacks, such as <see cref="OnGraphsUpdated"/>, run.
	/// - During the last step of a graph's scanning process. See <see cref="ScanningStage"/>.
	///
	/// To use e.g. AstarPath.active.GetNearest from an ECS job, you'll need to acquire a read lock first, and make sure the lock is only released when the job is finished.
	///
	/// <code>
	/// var readLock = AstarPath.active.LockGraphDataForReading();
	/// var handle = new MyJob {
	///     // ...
	/// }.Schedule(readLock.dependency);
	/// readLock.UnlockAfter(handle);
	/// </code>
	///
	/// See: <see cref="LockGraphDataForReading"/>
	/// </summary>
	RWLock graphDataLock = new RWLock();

	bool graphUpdateRoutineRunning = false;

	/// <summary>Makes sure QueueGraphUpdates will not queue multiple graph update orders</summary>
	bool graphUpdatesWorkItemAdded = false;

	/// <summary>
	/// Time the last graph update was done.
	/// Used to group together frequent graph updates to batches
	/// </summary>
	float lastGraphUpdate = -9999F;

	/// <summary>Held if any work items are currently queued</summary>
	PathProcessor.GraphUpdateLock workItemLock;

	/// <summary>Holds all completed paths waiting to be returned to where they were requested</summary>
	internal readonly PathReturnQueue pathReturnQueue;

	/// <summary>
	/// Holds settings for heuristic optimization.
	/// See: heuristic-opt (view in online documentation for working links)
	/// </summary>
	public EuclideanEmbedding euclideanEmbedding = new EuclideanEmbedding();

	/// <summary>
	/// If an async scan is running, this will be set to the coroutine.
	///
	/// This primarily used to be able to force the async scan to complete immediately,
	/// if the AstarPath component should happen to be destroyed while an async scan is running.
	/// </summary>
	IEnumerator<Progress> asyncScanTask;

	#endregion

	/// <summary>
	/// Shows or hides graph inspectors.
	/// Used internally by the editor
	/// </summary>
	public bool showGraphs = false;

	/// <summary>
	/// The next unused Path ID.
	/// Incremented for every call to GetNextPathID
	/// </summary>
	private ushort nextFreePathID = 1;
	bool suppressOnEnableOnDisable;

	private AstarPath () {
    }

    /// <summary>
    /// Returns tag names.
    /// Makes sure that the tag names array is not null and of length 32.
    /// If it is null or not of length 32, it creates a new array and fills it with 0,1,2,3,4 etc...
    /// See: AstarPath.FindTagNames
    /// </summary>
    public string[] GetTagNames()
    {
        return default;
    }

    /// <summary>
    /// Used outside of play mode to initialize the AstarPath object even if it has not been selected in the inspector yet.
    /// This will set the <see cref="active"/> property and deserialize all graphs.
    ///
    /// This is useful if you want to do changes to the graphs in the editor outside of play mode, but cannot be sure that the graphs have been deserialized yet.
    /// In play mode this method does nothing.
    /// </summary>
    public static void FindAstarPath()
    {
    }

    /// <summary>
    /// Tries to find an AstarPath object and return tag names.
    /// If an AstarPath object cannot be found, it returns an array of length 1 with an error message.
    /// See: AstarPath.GetTagNames
    /// </summary>
    public static string[] FindTagNames()
    {
        return default;
    }

    /// <summary>Returns the next free path ID</summary>
    internal ushort GetNextPathID()
    {
        return default;
    }

    void RecalculateDebugLimits()
    {
    }

    RedrawScope redrawScope;

    /// <summary>Calls OnDrawGizmos on all graphs</summary>
    public override void DrawGizmos()
    {
    }

#if !ASTAR_NO_GUI
    /// <summary>
    /// Draws the InGame debugging (if enabled)
    /// See: <see cref="logPathResults"/> PathLog
    /// </summary>
    private void OnGUI()
    {
    }
#endif

    /// <summary>
    /// Prints path results to the log. What it prints can be controled using <see cref="logPathResults"/>.
    /// See: <see cref="logPathResults"/>
    /// See: PathLog
    /// See: Pathfinding.Path.DebugString
    /// </summary>
    private void LogPathResults(Path path)
    {
    }

    /// <summary>
    /// Checks if any work items need to be executed
    /// then runs pathfinding for a while (if not using multithreading because
    /// then the calculation happens in other threads)
    /// and then returns any calculated paths to the
    /// scripts that requested them.
    ///
    /// See: PerformBlockingActions
    /// See: PathProcessor.TickNonMultithreaded
    /// See: PathReturnQueue.ReturnPaths
    /// </summary>
    private void Update()
    {
    }

    private void PreUpdateNavmeshUpdates()
    {
    }

    private void PerformBlockingActions(bool force = false)
    {
    }

    /// <summary>
    /// Add a work item to be processed when pathfinding is paused.
    ///
    /// The callback will be called once when it is safe to update graphs.
    ///
    /// This is a convenience method that is equivalent to
    /// <code>
    /// AddWorkItem(new AstarWorkItem(callback));
    /// </code>
    ///
    /// See: <see cref="AddWorkItem(AstarWorkItem)"/>
    /// </summary>
    public void AddWorkItem(System.Action callback)
    {
    }

    /// <summary>
    /// Add a work item to be processed when pathfinding is paused.
    ///
    /// THe callback will be called once when it is safe to update graphs.
    ///
    /// This is a convenience method that is equivalent to
    /// <code>
    /// AddWorkItem(new AstarWorkItem(callback));
    /// </code>
    ///
    /// See: <see cref="AddWorkItem(AstarWorkItem)"/>
    /// </summary>
    public void AddWorkItem(System.Action<IWorkItemContext> callback)
    {
    }

    /// <summary>
    /// Add a work item to be processed when pathfinding is paused.
    ///
    /// The work item will be executed when it is safe to update nodes. This is defined as between the path searches.
    /// When using more threads than one, calling this often might decrease pathfinding performance due to a lot of idling in the threads.
    /// Not performance as in it will use much CPU power, but performance as in the number of paths per second will probably go down
    /// (though your framerate might actually increase a tiny bit).
    ///
    /// You should only call this function from the main unity thread (i.e normal game code).
    ///
    /// <code>
    /// AstarPath.active.AddWorkItem(new AstarWorkItem(() => {
    ///     // Safe to update graphs here
    ///     var node = AstarPath.active.GetNearest(transform.position).node;
    ///     node.Walkable = false;
    /// }));
    /// </code>
    ///
    /// <code>
    /// AstarPath.active.AddWorkItem(() => {
    ///     // Safe to update graphs here
    ///     var node = AstarPath.active.GetNearest(transform.position).node;
    ///     node.position = (Int3)transform.position;
    /// });
    /// </code>
    ///
    /// You can run work items over multiple frames:
    /// <code>
    /// AstarPath.active.AddWorkItem(new AstarWorkItem(() => {
    ///     // Called once, right before the
    ///     // first call to the method below
    /// },
    ///     force => {
    ///     // Called every frame until complete.
    ///     // Signal that the work item is
    ///     // complete by returning true.
    ///     // The "force" parameter will
    ///     // be true if the work item is
    ///     // required to complete immediately.
    ///     // In that case this method should
    ///     // block and return true when done.
    ///     return true;
    /// }));
    /// </code>
    ///
    /// See: <see cref="FlushWorkItems"/>
    /// </summary>
    public void AddWorkItem(AstarWorkItem item)
    {
    }

    #region GraphUpdateMethods

    /// <summary>
    /// Will apply queued graph updates as soon as possible, regardless of <see cref="batchGraphUpdates"/>.
    /// Calling this multiple times will not create multiple callbacks.
    /// This function is useful if you are limiting graph updates, but you want a specific graph update to be applied as soon as possible regardless of the time limit.
    /// Note that this does not block until the updates are done, it merely bypasses the <see cref="batchGraphUpdates"/> time limit.
    ///
    /// See: <see cref="FlushGraphUpdates"/>
    /// </summary>
    public void QueueGraphUpdates()
    {
    }

    /// <summary>
    /// Waits a moment with updating graphs.
    /// If batchGraphUpdates is set, we want to keep some space between them to let pathfinding threads running and then calculate all queued calls at once
    /// </summary>
    IEnumerator DelayedGraphUpdate()
    {
        return default;
    }

    /// <summary>
    /// Update all graphs within bounds after delay seconds.
    /// The graphs will be updated as soon as possible.
    ///
    /// See: FlushGraphUpdates
    /// See: batchGraphUpdates
    /// See: graph-updates (view in online documentation for working links)
    /// </summary>
    public void UpdateGraphs(Bounds bounds, float delay)
    {
    }

    /// <summary>
    /// Update all graphs using the GraphUpdateObject after delay seconds.
    /// This can be used to, e.g make all nodes in a region unwalkable, or set them to a higher penalty.
    ///
    /// See: FlushGraphUpdates
    /// See: batchGraphUpdates
    /// See: graph-updates (view in online documentation for working links)
    /// </summary>
    public void UpdateGraphs(GraphUpdateObject ob, float delay)
    {
    }

    /// <summary>Update all graphs using the GraphUpdateObject after delay seconds</summary>
    IEnumerator UpdateGraphsInternal(GraphUpdateObject ob, float delay)
    {
        return default;
    }

    /// <summary>
    /// Update all graphs within bounds.
    /// The graphs will be updated as soon as possible.
    ///
    /// This is equivalent to
    /// <code>
    /// UpdateGraphs(new GraphUpdateObject(bounds));
    /// </code>
    ///
    /// See: FlushGraphUpdates
    /// See: batchGraphUpdates
    /// See: graph-updates (view in online documentation for working links)
    /// </summary>
    public void UpdateGraphs(Bounds bounds)
    {
    }

    /// <summary>
    /// Update all graphs using the GraphUpdateObject.
    /// This can be used to, e.g make all nodes in a region unwalkable, or set them to a higher penalty.
    /// The graphs will be updated as soon as possible (with respect to <see cref="batchGraphUpdates)"/>
    ///
    /// See: FlushGraphUpdates
    /// See: batchGraphUpdates
    /// See: graph-updates (view in online documentation for working links)
    /// </summary>
    public void UpdateGraphs(GraphUpdateObject ob)
    {
    }

    /// <summary>
    /// Forces graph updates to complete in a single frame.
    /// This will force the pathfinding threads to finish calculating the path they are currently calculating (if any) and then pause.
    /// When all threads have paused, graph updates will be performed.
    /// Warning: Using this very often (many times per second) can reduce your fps due to a lot of threads waiting for one another.
    /// But you probably wont have to worry about that.
    ///
    /// Note: This is almost identical to <see cref="FlushWorkItems"/>, but added for more descriptive name.
    /// This function will also override any time limit delays for graph updates.
    /// This is because graph updates are implemented using work items.
    /// So calling this function will also execute any other work items (if any are queued).
    ///
    /// Will not do anything if there are no graph updates queued (not even execute other work items).
    /// </summary>
    public void FlushGraphUpdates ()
    {
    }

    #endregion

    /// <summary>
    /// Forces work items to complete in a single frame.
    /// This will force all work items to run immidiately.
    /// This will force the pathfinding threads to finish calculating the path they are currently calculating (if any) and then pause.
    /// When all threads have paused, work items will be executed (which can be e.g graph updates).
    ///
    /// Warning: Using this very often (many times per second) can reduce your fps due to a lot of threads waiting for one another.
    /// But you probably wont have to worry about that
    ///
    /// Note: This is almost (note almost) identical to <see cref="FlushGraphUpdates"/>, but added for more descriptive name.
    ///
    /// Will not do anything if there are no queued work items waiting to run.
    /// </summary>
    public void FlushWorkItems()
    {
    }

    /// <summary>
    /// Calculates number of threads to use.
    /// If count is not Automatic, simply returns count casted to an int.
    /// Returns: An int specifying how many threads to use, 0 means a coroutine should be used for pathfinding instead of a separate thread.
    ///
    /// If count is set to Automatic it will return a value based on the number of processors and memory for the current system.
    /// If memory is <= 512MB or logical cores are <= 1, it will return 0. If memory is <= 1024 it will clamp threads to max 2.
    /// Otherwise it will return the number of logical cores clamped to 6.
    ///
    /// When running on WebGL this method always returns 0
    /// </summary>
    public static int CalculateThreadCount(ThreadCount count)
    {
        return default;
    }

    /// <summary>Initializes the <see cref="pathProcessor"/> field</summary>
    void InitializePathProcessor()
    {
    }

    void InitializeColors()
    {
    }

    void ShutdownPathfindingThreads()
    {
    }

    /// <summary>
    /// Called after this component is enabled.
    ///
    /// Unless the component has already been activated in Awake, this method should:
    /// - Ensure the singleton holds (setting <see cref="active"/> to this).
    /// - Make sure all subsystems that were disabled in OnDisable are again enabled.
    ///   - This includes starting pathfinding threads.
    /// </summary>
    void OnEnable()
    {
    }

    public void FastEnable()
    {
    }

    public void FastDisable()
    {
    }

    /// <summary>
    /// Cleans up graphs to avoid memory leaks.
    ///
    /// This is called by Unity when:
    /// - The component is explicitly disabled in play mode or editor mode.
    /// - When the component is about to be destroyed
    ///   - Including when the game stops
    /// - When an undo/redo event takes place (Unity will first disable the component and then enable it again).
    ///
    /// During edit and play mode this method should:
    /// - Destroy all node data (but not the graphs themselves)
    /// - Dispose all unmanaged data
    /// - Shutdown pathfinding threads if they are running (any pending path requests are left in the queue)
    /// </summary>
    void OnDisable()
    {
    }

    /// <summary>
    /// Clears up variables and other stuff, destroys graphs.
    /// Note that when destroying an AstarPath object, all static variables such as callbacks will be cleared.
    /// </summary>
    void OnDestroy()
    {
    }

    #region ScanMethods

    /// <summary>
    /// Allocate a bunch of nodes at once.
    /// This is faster than allocating each individual node separately and it can be done in a separate thread by using jobs.
    ///
    /// <code>
    /// var nodes = new PointNode[128];
    /// var job = AstarPath.active.AllocateNodes(nodes, 128, () => new PointNode(), 1);
    ///
    /// job.Complete();
    /// </code>
    ///
    /// See: <see cref="InitializeNode"/>
    /// </summary>
    /// <param name="result">Node array to fill</param>
    /// <param name="count">How many nodes to allocate</param>
    /// <param name="createNode">Delegate which creates a node. () => new T(). Note that new T(AstarPath.active) should *not* be used as that will cause the node to be initialized twice.</param>
    /// <param name="variantsPerNode">How many variants of the node to allocate. Should be the same as \reflink{GraphNode.PathNodeVariants} for this node type.</param>
    public Unity.Jobs.JobHandle AllocateNodes<T>(T[] result, int count, System.Func<T> createNode, uint variantsPerNode) where T : GraphNode
    {
        return default;
    }

    /// <summary>
    /// Initializes temporary path data for a node.
    ///
    /// Use like: InitializeNode(new PointNode())
    ///
    /// See: <see cref="AstarPath.AllocateNodes"/>
    /// </summary>
    internal void InitializeNode(GraphNode node)
    {
    }

    internal void InitializeNodes(GraphNode[] nodes)
    {
    }

    /// <summary>
    /// Internal method to destroy a given node.
    /// This is to be called after the node has been disconnected from the graph so that it cannot be reached from any other nodes.
    /// It should only be called during graph updates, that is when the pathfinding threads are either not running or paused.
    ///
    /// Warning: This method should not be called by user code. It is used internally by the system.
    /// </summary>
    internal void DestroyNode(GraphNode node)
    {
    }

    /// <summary>
    /// Blocks until all pathfinding threads are paused and blocked.
    ///
    /// <code>
    /// var graphLock = AstarPath.active.PausePathfinding();
    /// // Here we can modify the graphs safely. For example by increasing the penalty of a node
    /// AstarPath.active.data.gridGraph.GetNode(0, 0).Penalty += 1000;
    ///
    /// // Allow pathfinding to resume
    /// graphLock.Release();
    /// </code>
    ///
    /// Returns: A lock object. You need to call <see cref="Pathfinding.PathProcessor.GraphUpdateLock.Release"/> on that object to allow pathfinding to resume.
    /// Note: In most cases this should not be called from user code. Use the <see cref="AddWorkItem"/> method instead.
    ///
    /// See: <see cref="AddWorkItem"/>
    /// </summary>
    public PathProcessor.GraphUpdateLock PausePathfinding()
    {
        return default;
    }

    /// <summary>
    /// Blocks the path queue so that e.g work items can be performed.
    ///
    /// Pathfinding threads will stop accepting new path requests and will finish the ones they are currently calculating asynchronously.
    /// When the lock is released, the pathfinding threads will resume as normal.
    ///
    /// Note: You are unlikely to need to use this method. It is primarily for internal use.
    /// </summary>
    public PathProcessor.GraphUpdateLock PausePathfindingSoon () {
        return default;
    }

    /// <summary>Blocks until the currently running async scan (if any) has completed</summary>
    void BlockUntilAsyncScanComplete()
    {
    }

    /// <summary>
    /// Scans a particular graph.
    /// Calling this method will recalculate the specified graph from scratch.
    /// This method is pretty slow (depending on graph type and graph complexity of course), so it is advisable to use
    /// smaller graph updates whenever possible.
    ///
    /// <code>
    /// // Recalculate all graphs
    /// AstarPath.active.Scan();
    ///
    /// // Recalculate only the first grid graph
    /// var graphToScan = AstarPath.active.data.gridGraph;
    /// AstarPath.active.Scan(graphToScan);
    ///
    /// // Recalculate only the first and third graphs
    /// var graphsToScan = new [] { AstarPath.active.data.graphs[0], AstarPath.active.data.graphs[2] };
    /// AstarPath.active.Scan(graphsToScan);
    /// </code>
    ///
    /// See: graph-updates (view in online documentation for working links)
    /// See: ScanAsync
    /// </summary>
    public void Scan(NavGraph graphToScan)
    {
    }

    /// <summary>
    /// Scans all specified graphs.
    ///
    /// Calling this method will recalculate all specified graphs (or all graphs if the graphsToScan parameter is null) from scratch.
    /// This method is pretty slow (depending on graph type and graph complexity of course), so it is advisable to use
    /// smaller graph updates whenever possible.
    ///
    /// <code>
    /// // Recalculate all graphs
    /// AstarPath.active.Scan();
    ///
    /// // Recalculate only the first grid graph
    /// var graphToScan = AstarPath.active.data.gridGraph;
    /// AstarPath.active.Scan(graphToScan);
    ///
    /// // Recalculate only the first and third graphs
    /// var graphsToScan = new [] { AstarPath.active.data.graphs[0], AstarPath.active.data.graphs[2] };
    /// AstarPath.active.Scan(graphsToScan);
    /// </code>
    ///
    /// See: graph-updates (view in online documentation for working links)
    /// See: ScanAsync
    /// </summary>
    /// <param name="graphsToScan">The graphs to scan. If this parameter is null then all graphs will be scanned</param>
    public void Scan(NavGraph[] graphsToScan = null)
    {
    }

    /// <summary>
    /// Scans a particular graph asynchronously. This is a IEnumerable, you can loop through it to get the progress
    ///
    /// You can scan graphs asyncronously by yielding when you iterate through the returned IEnumerable.
    /// Note that this does not guarantee a good framerate, but it will allow you
    /// to at least show a progress bar while scanning.
    ///
    /// <code>
    /// IEnumerator Start () {
    ///     foreach (Progress progress in AstarPath.active.ScanAsync()) {
    ///         Debug.Log("Scanning... " + progress.ToString());
    ///         yield return null;
    ///     }
    /// }
    /// </code>
    ///
    /// See: Scan
    /// </summary>
    public IEnumerable<Progress> ScanAsync(NavGraph graphToScan)
    {
        return default;
    }

    /// <summary>
    /// Scans all specified graphs asynchronously. This is a IEnumerable, you can loop through it to get the progress
    ///
    /// You can scan graphs asyncronously by yielding when you loop through the progress.
    /// Note that this does not guarantee a good framerate, but it will allow you
    /// to at least show a progress bar during scanning.
    ///
    /// <code>
    /// IEnumerator Start () {
    ///     foreach (Progress progress in AstarPath.active.ScanAsync()) {
    ///         Debug.Log("Scanning... " + progress.ToString());
    ///         yield return null;
    ///     }
    /// }
    /// </code>
    ///
    /// Note: If the graphs are already scanned, doing an async scan will temporarily cause increased memory usage, since two copies of the graphs will be kept in memory during the async scan.
    /// This may not be desirable on some platforms. A non-async scan will not cause this temporary increased memory usage.
    ///
    /// See: Scan
    /// </summary>
    /// <param name="graphsToScan">The graphs to scan. If this parameter is null then all graphs will be scanned</param>
    public IEnumerable<Progress> ScanAsync(NavGraph[] graphsToScan = null)
    {
        return default;
    }

    IEnumerable<Progress> TickAsyncScanUntilCompletion(IEnumerator<Progress> task)
    {
        return default;
    }

    class DummyGraphUpdateContext : IGraphUpdateContext
    {
        public void DirtyBounds(Bounds bounds)
        {
        }
    }

    class DestroyGraphPromise : IGraphUpdatePromise
    {
        public IGraphInternals graph;
        public IEnumerator<JobHandle> Prepare()
        {
            return default;
        }

        public void Apply(IGraphUpdateContext context)
        {
        }
    }

    IEnumerable<Progress> ScanInternal(NavGraph[] graphsToScan, bool async)
    {
        return default;
    }

    #endregion

    internal void DirtyBounds(Bounds bounds)
    {
    }

    private static int waitForPathDepth = 0;

    /// <summary>
    /// Blocks until the path has been calculated.
    ///
    /// Normally it takes a few frames for a path to be calculated and returned.
    /// This function will ensure that the path will be calculated when this function returns
    /// and that the callback for that path has been called.
    ///
    /// If requesting a lot of paths in one go and waiting for the last one to complete,
    /// it will calculate most of the paths in the queue (only most if using multithreading, all if not using multithreading).
    ///
    /// Use this function only if you really need to.
    /// There is a point to spreading path calculations out over several frames.
    /// It smoothes out the framerate and makes sure requesting a large
    /// number of paths at the same time does not cause lag.
    ///
    /// Note: Graph updates and other callbacks might get called during the execution of this function.
    ///
    /// When the pathfinder is shutting down. I.e in OnDestroy, this function will not do anything.
    ///
    /// Throws: Exception if pathfinding is not initialized properly for this scene (most likely no AstarPath object exists)
    /// or if the path has not been started yet.
    /// Also throws an exception if critical errors occur such as when the pathfinding threads have crashed (which should not happen in normal cases).
    /// This prevents an infinite loop while waiting for the path.
    ///
    /// See: Pathfinding.Path.WaitForPath
    /// See: Pathfinding.Path.BlockUntilCalculated
    /// </summary>
    /// <param name="path">The path to wait for. The path must be started, otherwise an exception will be thrown.</param>
    public static void BlockUntilCalculated (Path path) {
    }

    /// <summary>
    /// Adds the path to a queue so that it will be calculated as soon as possible.
    /// The callback specified when constructing the path will be called when the path has been calculated.
    /// Usually you should use the Seeker component instead of calling this function directly.
    ///
    /// <code>
    /// // There must be an AstarPath instance in the scene
    /// if (AstarPath.active == null) return;
    ///
    /// // We can calculate multiple paths asynchronously
    /// for (int i = 0; i < 10; i++) {
    ///     var path = ABPath.Construct(transform.position, transform.position+transform.forward*i*10, OnPathComplete);
    ///
    ///     // Calculate the path by using the AstarPath component directly
    ///     AstarPath.StartPath(path);
    /// }
    /// </code>
    /// </summary>
    /// <param name="path">The path that should be enqueued.</param>
    /// <param name="pushToFront">If true, the path will be pushed to the front of the queue, bypassing all waiting paths and making it the next path to be calculated.
    ///    This can be useful if you have a path which you want to prioritize over all others. Be careful to not overuse it though.
    ///    If too many paths are put in the front of the queue often, this can lead to normal paths having to wait a very long time before being calculated.</param>
    /// <param name="assumeInPlayMode">Typically path.BlockUntilCalculated will be called when not in play mode. However, the play mode check will not work if
    ///    you call this from a separate thread, or a job. In that case you can set this to true to skip the check.</param>
    public static void StartPath(Path path, bool pushToFront = false, bool assumeInPlayMode = false)
    {
    }

    /// <summary>
    /// Cached NNConstraint to avoid unnecessary allocations.
    /// This should ideally be fixed by making NNConstraint an immutable class/struct.
    /// </summary>
    internal static readonly NNConstraint NNConstraintClosestAsSeenFromAbove = new NNConstraint()
    {
        constrainWalkability = false,
        constrainTags = false,
        constrainDistance = true,
        distanceMetric = DistanceMetric.ClosestAsSeenFromAbove(),
    };

    /// <summary>
    /// True if the point is on a walkable part of the navmesh, as seen from above.
    ///
    /// A point is considered on the navmesh if it is above or below a walkable navmesh surface, at any distance,
    /// and if it is not above/below a closer unwalkable node.
    ///
    /// Note: This means that, for example, in multi-story building a point will be considered on the navmesh if any walkable floor is below or above the point.
    /// If you want more complex behavior then you can use the GetNearest method together with the appropriate <see cref="NNConstraint.distanceMetric"/> settings for your use case.
    ///
    /// This uses the graph's natural up direction to determine which way is up.
    /// Therefore, it will also work on rotated graphs, as well as graphs in 2D mode.
    ///
    /// This method works for all graph types.
    /// However, for <see cref="PointGraph"/>s, this will never return true unless you pass in the exact coordinate of a node, since point nodes do not have a surface.
    ///
    /// Note: For spherical navmeshes (or other weird shapes), this method will not work as expected, as there's no well defined "up" direction.
    ///
    /// [Open online documentation to see images]
    ///
    /// See: <see cref="NavGraph.IsPointOnNavmesh"/> to check if a point is on the navmesh of a specific graph.
    /// </summary>
    /// <param name="position">The point to check</param>
    public bool IsPointOnNavmesh(Vector3 position)
    {
        return default;
    }

    /// <summary>
    /// Returns the nearest node to a position.
    /// This method will search through all graphs and query them for the closest node to this position, and then it will return the closest one of those.
    ///
    /// Equivalent to GetNearest(position, NNConstraint.None).
    ///
    /// <code>
    /// // Find the closest node to this GameObject's position
    /// GraphNode node = AstarPath.active.GetNearest(transform.position).node;
    ///
    /// if (node.Walkable) {
    ///     // Yay, the node is walkable, we can place a tower here or something
    /// }
    /// </code>
    ///
    /// See: Pathfinding.NNConstraint
    /// </summary>
    public NNInfo GetNearest(Vector3 position)
    {
        return default;
    }

    /// <summary>
    /// Returns the nearest node to a point using the specified NNConstraint.
    ///
    /// Searches through all graphs for their nearest nodes to the specified position and picks the closest one.
    /// The NNConstraint can be used to specify constraints on which nodes can be chosen such as only picking walkable nodes.
    ///
    /// <code>
    /// GraphNode node = AstarPath.active.GetNearest(transform.position, NNConstraint.Walkable).node;
    /// </code>
    ///
    /// <code>
    /// var constraint = NNConstraint.None;
    ///
    /// // Constrain the search to walkable nodes only
    /// constraint.constrainWalkability = true;
    /// constraint.walkable = true;
    ///
    /// // Constrain the search to only nodes with tag 3 or tag 5
    /// // The 'tags' field is a bitmask
    /// constraint.constrainTags = true;
    /// constraint.tags = (1 << 3) | (1 << 5);
    ///
    /// var info = AstarPath.active.GetNearest(transform.position, constraint);
    /// var node = info.node;
    /// var closestPoint = info.position;
    /// </code>
    ///
    /// See: <see cref="NNConstraint"/>
    /// </summary>
    /// <param name="position">The point to find nodes close to</param>
    /// <param name="constraint">The constraint which determines which graphs and nodes are acceptable to search on. May be null, in which case all nodes will be considered acceptable.</param>
    public NNInfo GetNearest(Vector3 position, NNConstraint constraint)
    {
        return default;
    }

    /// <summary>
    /// True if there is an obstacle between start and end on the navmesh.
    ///
    /// This is a simple api to check if there is an obstacle between two points.
    /// If you need more detailed information, you can use <see cref="GridGraph.Linecast"/> or <see cref="NavmeshBase.Linecast"/> (for navmesh/recast graphs).
    /// Those overloads can also return which nodes the line passed through, and allow you use custom node filtering.
    ///
    /// <code>
    /// var start = transform.position;
    /// var end = start + Vector3.forward * 10;
    /// if (AstarPath.active.Linecast(start, end)) {
    ///     Debug.DrawLine(start, end, Color.red);
    /// } else {
    ///     Debug.DrawLine(start, end, Color.green);
    /// }
    /// </code>
    ///
    /// Note: Only grid, recast and navmesh graphs support linecasts. The closest raycastable graph to the start point will be used for the linecast.
    /// Note: Linecasts cannot pass through off-mesh links.
    ///
    /// See: <see cref="NavmeshBase.Linecast"/>
    /// See: <see cref="GridGraph.Linecast"/>
    /// See: <see cref="IRaycastableGraph"/>
    /// See: linecasting (view in online documentation for working links), for more details about linecasting
    /// </summary>
    public bool Linecast(Vector3 start, Vector3 end)
    {
        return default;
    }

    /// <summary>
    /// True if there is an obstacle between start and end on the navmesh.
    ///
    /// This is a simple api to check if there is an obstacle between two points.
    /// If you need more detailed information, you can use <see cref="GridGraph.Linecast"/> or <see cref="NavmeshBase.Linecast"/> (for navmesh/recast graphs).
    /// Those overloads can also return which nodes the line passed through, and allow you use custom node filtering.
    ///
    /// <code>
    /// var start = transform.position;
    /// var end = start + Vector3.forward * 10;
    /// if (AstarPath.active.Linecast(start, end, out var hit)) {
    ///     Debug.DrawLine(start, end, Color.red);
    ///     Debug.DrawRay(hit.point, Vector3.up, Color.red);
    /// } else {
    ///     Debug.DrawLine(start, end, Color.green);
    /// }
    /// </code>
    ///
    /// Note: Only grid, recast and navmesh graphs support linecasts. The closest raycastable graph to the start point will be used for the linecast.
    /// Note: Linecasts cannot pass through off-mesh links.
    ///
    /// See: <see cref="NavmeshBase.Linecast"/>
    /// See: <see cref="GridGraph.Linecast"/>
    /// See: <see cref="IRaycastableGraph"/>
    /// See: linecasting (view in online documentation for working links), for more details about linecasting
    /// </summary>
    public bool Linecast(Vector3 start, Vector3 end, out GraphHitInfo hit)
    {
        hit = default(GraphHitInfo);
        return default;
    }

    IRaycastableGraph ClosestRaycastableGraph(Vector3 point)
    {
        return default;
    }

    /// <summary>
    /// Returns the node closest to the ray (slow).
    /// Warning: This function is brute-force and very slow, use with caution
    /// </summary>
    public GraphNode GetNearest(Ray ray)
    {
        return default;
    }

    /// <summary>
    /// Captures a snapshot of a part of the graphs, to allow restoring it later.
    ///
    /// This is useful if you want to do a graph update, but you want to be able to restore the graph to the previous state.
    ///
    /// The snapshot will capture enough information to restore the graphs, assuming the world only changed within the given bounding box.
    /// This means the captured region may be larger than the bounding box.
    ///
    /// <b>Limitations:</b>
    /// - Currently, the <see cref="GridGraph"/> and <see cref="LayerGridGraph"/> supports snapshots. Other graph types do not support it.
    /// - The graph must not change its dimensions or other core parameters between the time the snapshot is taken and the time it is restored.
    /// - Custom node connections may not be preserved. Unless they are added as off-mesh links using e.g. a <see cref="NodeLink2"/> component.
    /// - The snapshot must not be captured during a work item, graph update or when the graphs are being scanned, as the graphs may not be in a consistent state during those times.
    ///
    /// See: <see cref="GraphUpdateUtilities.UpdateGraphsNoBlock"/>, which uses this method internally.
    /// See: <see cref="NavGraph.Snapshot"/>
    ///
    /// Note: You must dispose the returned snapshot when you are done with it, to avoid leaking memory.
    /// </summary>
    public GraphSnapshot Snapshot(Bounds bounds, GraphMask graphMask)
    {
        return default;
    }

    /// <summary>
    /// Allows you to access read-only graph data in jobs safely.
    ///
    /// You can for example use AstarPath.active.GetNearest(...) in a job.
    ///
    /// Using <see cref="AstarPath.StartPath"/> is always safe to use in jobs even without calling this method.
    ///
    /// When a graph update, work item, or graph scan would start, it will first block on the given dependency
    /// to ensure no race conditions occur.
    ///
    /// If you do not call this method, then a graph update might start in the middle of your job, causing race conditions
    /// and all manner of other hard-to-diagnose bugs.
    ///
    /// <code>
    /// var readLock = AstarPath.active.LockGraphDataForReading();
    /// var handle = new MyJob {
    ///     // ...
    /// }.Schedule(readLock.dependency);
    /// readLock.UnlockAfter(handle);
    /// </code>
    ///
    /// See: <see cref="LockGraphDataForWriting"/>
    /// See: <see cref="graphDataLock"/>
    /// </summary>
    public RWLock.ReadLockAsync LockGraphDataForReading() => graphDataLock.Read();

    /// <summary>
    /// Aquires an exclusive lock on the graph data asynchronously.
    /// This is used when graphs want to modify graph data.
    ///
    /// This is a low-level primitive, usually you do not need to use this method.
    ///
    /// <code>
    /// var readLock = AstarPath.active.LockGraphDataForReading();
    /// var handle = new MyJob {
    ///     // ...
    /// }.Schedule(readLock.dependency);
    /// readLock.UnlockAfter(handle);
    /// </code>
    ///
    /// See: <see cref="LockGraphDataForReading"/>
    /// See: <see cref="graphDataLock"/>
    /// </summary>
    public RWLock.WriteLockAsync LockGraphDataForWriting() => graphDataLock.Write();

    /// <summary>
    /// Aquires an exclusive lock on the graph data.
    /// This is used when graphs want to modify graph data.
    ///
    /// This is a low-level primitive, usually you do not need to use this method.
    ///
    /// <code>
    /// var readLock = AstarPath.active.LockGraphDataForReading();
    /// var handle = new MyJob {
    ///     // ...
    /// }.Schedule(readLock.dependency);
    /// readLock.UnlockAfter(handle);
    /// </code>
    ///
    /// See: <see cref="LockGraphDataForReading"/>
    /// See: <see cref="graphDataLock"/>
    /// </summary>
    public RWLock.LockSync LockGraphDataForWritingSync() => graphDataLock.WriteSync();

    /// <summary>
    /// Obstacle data for navmesh edges.
    ///
    /// This can be used to get information about the edge/borders of the navmesh.
    /// It can also be queried in burst jobs. Just make sure you release the read lock after you are done with it.
    ///
    /// Note: This is not a method that you are likely to need to use.
    /// It is used internally for things like local avoidance.
    /// </summary>
    public NavmeshEdges.NavmeshBorderData GetNavmeshBorderData(out RWLock.CombinedReadLockAsync readLock) => hierarchicalGraph.navmeshEdges.GetNavmeshEdgeData(out readLock);

    class AstarPathPreUpdate
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void Initialize()
        {
        }

        static void PreUpdate()
        {
        }
    }
}

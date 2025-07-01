//#define ProfileAstar

using UnityEngine;
using System.Text;
using Pathfinding.Pooling;

namespace Pathfinding {
	[AddComponentMenu("Pathfinding/Pathfinding Debugger")]
	[ExecuteInEditMode]
	/// <summary>
	/// Debugger for the A* Pathfinding Project.
	/// This class can be used to profile different parts of the pathfinding system
	/// and the whole game as well to some extent.
	///
	/// Clarification of the labels shown when enabled.
	/// All memory related things profiles <b>the whole game</b> not just the A* Pathfinding System.
	/// - Currently allocated: memory the GC (garbage collector) says the application has allocated right now.
	/// - Peak allocated: maximum measured value of the above.
	/// - Last collect peak: the last peak of 'currently allocated'.
	/// - Allocation rate: how much the 'currently allocated' value increases per second. This value is not as reliable as you can think
	/// it is often very random probably depending on how the GC thinks this application is using memory.
	/// - Collection frequency: how often the GC is called. Again, the GC might decide it is better with many small collections
	/// or with a few large collections. So you cannot really trust this variable much.
	/// - Last collect fps: FPS during the last garbage collection, the GC will lower the fps a lot.
	///
	/// - FPS: current FPS (not updated every frame for readability)
	/// - Lowest FPS (last x): As the label says, the lowest fps of the last x frames.
	///
	/// - Size: Size of the path pool.
	/// - Total created: Number of paths of that type which has been created. Pooled paths are not counted twice.
	/// If this value just keeps on growing and growing without an apparent stop, you are are either not pooling any paths
	/// or you have missed to pool some path somewhere in your code.
	///
	/// See: pooling
	///
	/// TODO: Add field showing how many graph updates are being done right now
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/astardebugger.html")]
	public class AstarDebugger : VersionedMonoBehaviour {
		public int yOffset = 5;

		public bool show = true;
		public bool showInEditor = false;

		public bool showFPS = false;
		public bool showPathProfile = false;
		public bool showMemProfile = false;
		public bool showGraph = false;

		public int graphBufferSize = 200;

		/// <summary>
		/// Font to use.
		/// A monospaced font is the best
		/// </summary>
		public Font font = null;
		public int fontSize = 12;

		StringBuilder text = new StringBuilder();
		string cachedText;
		float lastUpdate = -999;

		private GraphPoint[] graph;

		struct GraphPoint {
			public float fps, memory;
			public bool collectEvent;
		}

		private float delayedDeltaTime = 1;
		private float lastCollect = 0;
		private float lastCollectNum = 0;
		private float delta = 0;
		private float lastDeltaTime = 0;
		private int allocRate = 0;
		private int lastAllocMemory = 0;
		private float lastAllocSet = -9999;
		private int allocMem = 0;
		private int collectAlloc = 0;
		private int peakAlloc = 0;

		private int fpsDropCounterSize = 200;
		private float[] fpsDrops;

		private Rect boxRect;

		private GUIStyle style;

		private Camera cam;

		float graphWidth = 100;
		float graphHeight = 100;
		float graphOffset = 50;

		public void Start ()
        {
        }

        int maxVecPool = 0;
        int maxNodePool = 0;

        PathTypeDebug[] debugTypes = new PathTypeDebug[] {
            new PathTypeDebug("ABPath", () => PathPool.GetSize(typeof(ABPath)), () => PathPool.GetTotalCreated(typeof(ABPath)))
            ,
            new PathTypeDebug("MultiTargetPath", () => PathPool.GetSize(typeof(MultiTargetPath)), () => PathPool.GetTotalCreated(typeof(MultiTargetPath))),
            new PathTypeDebug("RandomPath", () => PathPool.GetSize(typeof(RandomPath)), () => PathPool.GetTotalCreated(typeof(RandomPath))),
            new PathTypeDebug("FleePath", () => PathPool.GetSize(typeof(FleePath)), () => PathPool.GetTotalCreated(typeof(FleePath))),
            new PathTypeDebug("ConstantPath", () => PathPool.GetSize(typeof(ConstantPath)), () => PathPool.GetTotalCreated(typeof(ConstantPath))),
            new PathTypeDebug("FloodPath", () => PathPool.GetSize(typeof(FloodPath)), () => PathPool.GetTotalCreated(typeof(FloodPath))),
            new PathTypeDebug("FloodPathTracer", () => PathPool.GetSize(typeof(FloodPathTracer)), () => PathPool.GetTotalCreated(typeof(FloodPathTracer)))
        };

        struct PathTypeDebug
        {
            string name;
			System.Func<int> getSize;
			System.Func<int> getTotalCreated;
			public PathTypeDebug (string name, System.Func<int> getSize, System.Func<int> getTotalCreated) : this()
            {
            }

            public void Print(StringBuilder text)
            {
            }
        }

        public void LateUpdate()
        {
        }

        void DrawGraphLine(int index, Matrix4x4 m, float x1, float x2, float y1, float y2, Color color)
        {
        }

        public void OnGUI () {
        }
    }
}

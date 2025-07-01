using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding.Examples {
	/// <summary>Helper script in the example scene 'Turn Based'</summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/turnbasedai.html")]
	public class TurnBasedAI : VersionedMonoBehaviour {
		public int movementPoints = 2;
		public BlockManager blockManager;
		public SingleNodeBlocker blocker;
		public GraphNode targetNode;
		public BlockManager.TraversalProvider traversalProvider;

		void Start () {
        }

        protected override void Awake () {
        }
    }
}

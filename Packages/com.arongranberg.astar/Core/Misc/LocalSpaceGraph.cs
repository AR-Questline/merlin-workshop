using UnityEngine;
namespace Pathfinding {
	using Pathfinding.Util;

	/// <summary>Helper for <see cref="Pathfinding.Examples.LocalSpaceRichAI"/></summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/localspacegraph.html")]
	public class LocalSpaceGraph : VersionedMonoBehaviour {
		Matrix4x4 originalMatrix;
		MutableGraphTransform graphTransform = new MutableGraphTransform(Matrix4x4.identity);
		public GraphTransform transformation { get { return graphTransform; } }

		void Start () {
        }

        public void Refresh () {
        }
    }
}

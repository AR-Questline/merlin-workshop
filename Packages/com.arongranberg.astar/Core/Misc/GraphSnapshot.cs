using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Pathfinding.Util {
	public interface IGraphSnapshot : System.IDisposable {
		/// <summary>
		/// Restores the graph data to the state it had when the snapshot was taken, in the bounding box that the snapshot captured.
		///
		/// You can get the context from the callback provided to the <see cref="AstarPath.AddWorkItem"/> method.
		/// </summary>
		void Restore(IGraphUpdateContext ctx);
	}

	/// <summary>
	/// A snapshot of parts of graphs.
	///
	/// See: <see cref="AstarPath.Snapshot"/>
	/// </summary>
	public struct GraphSnapshot : IGraphSnapshot {
		List<IGraphSnapshot> inner;

		internal GraphSnapshot (List<IGraphSnapshot> inner) : this()
        {
        }

        /// <summary>\copydocref{IGraphSnapshot.Restore}</summary>
        public void Restore (IGraphUpdateContext ctx) {
        }

        public void Dispose () {
        }
    }
}

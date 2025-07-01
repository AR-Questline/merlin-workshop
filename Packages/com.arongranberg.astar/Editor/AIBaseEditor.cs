using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	[CustomEditor(typeof(AIBase), true)]
	[CanEditMultipleObjects]
	public class BaseAIEditor : EditorBase {
		float lastSeenCustomGravity = float.NegativeInfinity;
		bool debug = false;

		protected void AutoRepathInspector () {
        }

        protected void DebugInspector () {
        }

        protected override void Inspector () {
        }
    }
}

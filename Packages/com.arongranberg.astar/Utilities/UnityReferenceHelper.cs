using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding {
	[ExecuteInEditMode]
	/// <summary>
	/// Helper class to keep track of references to GameObjects.
	/// Does nothing more than to hold a GUID value.
	/// </summary>
	[HelpURL("https://arongranberg.com/astar/documentation/stable/unityreferencehelper.html")]
	public class UnityReferenceHelper : MonoBehaviour {
		[HideInInspector]
		[SerializeField]
		private string guid;

		public string GetGUID() => guid;

		public void Awake () {
        }

        public void Reset () {
        }
    }
}

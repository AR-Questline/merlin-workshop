using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Pathfinding {
	using Pathfinding.Sync;

	/// <summary>Editor for the <see cref="NavmeshPrefab"/> component</summary>
	[CustomEditor(typeof(NavmeshPrefab), true)]
	[CanEditMultipleObjects]
	public class NavmeshPrefabEditor : EditorBase {
		protected override void OnEnable () {
        }

        protected override void OnDisable () {
        }

        void OnUpdate () {
        }

        static int pendingScanProgressId;

        static int PendingScan(Promise<NavmeshPrefab.SerializedOutput>[] pendingScanProgress, NavmeshPrefab[] pendingScanTargets)
        {
            return default;
        }

        protected override void Inspector()
        {
        }
    }
}

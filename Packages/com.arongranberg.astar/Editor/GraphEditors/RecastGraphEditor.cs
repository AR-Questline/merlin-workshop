using UnityEngine;
using UnityEditor;
using Pathfinding.Graphs.Navmesh;
using UnityEditorInternal;

namespace Pathfinding {
	/// <summary>Editor for the RecastGraph.</summary>
	[CustomGraphEditor(typeof(RecastGraph), "Recast Graph")]
	public class RecastGraphEditor : GraphEditor {
		public static bool tagMaskFoldout;
		public static bool meshesUnreadableAtRuntimeFoldout;
		ReorderableList tagMaskList;
		ReorderableList perLayerModificationsList;

		public enum UseTiles {
			UseTiles = 0,
			DontUseTiles = 1
		}

		static readonly GUIContent[] DimensionModeLabels = new [] {
			new GUIContent("2D"),
			new GUIContent("3D"),
		};

		static Rect SliceColumn (ref Rect rect, float width, float spacing = 0) {
            return default;
        }

        static void DrawIndentedList(ReorderableList list)
        {
        }

        static void DrawColliderDetail(RecastGraph.CollectionSettings settings)
        {
        }

        void DrawCollectionSettings(RecastGraph.CollectionSettings settings, RecastGraph.DimensionMode dimensionMode)
        {
        }

        public override void OnEnable()
        {
        }

        public override void OnInspectorGUI(NavGraph target)
        {
        }

        static readonly Vector3[] handlePoints = new [] { new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(0, -1, 0) };

		public override void OnSceneGUI (NavGraph target) {
        }

        /// <summary>Exports the INavmesh graph to a .obj file</summary>
        public static void ExportToFile(NavmeshBase target)
        {
        }
    }
}

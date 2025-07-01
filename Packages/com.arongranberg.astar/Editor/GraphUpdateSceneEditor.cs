using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Pathfinding {
	/// <summary>Editor for GraphUpdateScene</summary>
	[CustomEditor(typeof(GraphUpdateScene))]
	[CanEditMultipleObjects]
	public class GraphUpdateSceneEditor : EditorBase {
		int selectedPoint = -1;

		const float pointGizmosRadius = 0.09F;
		static Color PointColor = new Color(1, 0.36F, 0, 0.6F);
		static Color PointSelectedColor = new Color(1, 0.24F, 0, 1.0F);

		GraphUpdateScene[] scripts;

		protected override void Inspector () {
        }

        void DrawPointsField()
        {
        }

        void DrawPhysicsField()
        {
        }

        void DrawConvexField()
        {
        }

        void DrawWalkableField()
        {
        }

        void DrawPenaltyField()
        {
        }

        void DrawTagField()
        {
        }

        static void SphereCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
        }

        public void OnSceneGUI()
        {
        }
    }
}

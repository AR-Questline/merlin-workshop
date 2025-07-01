using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Util;
	using Pathfinding.Drawing;

	[HelpURL("https://arongranberg.com/astar/documentation/stable/animationlink.html")]
	public class AnimationLink : NodeLink2 {
		public string clip;
		public float animSpeed = 1;
		public bool reverseAnim = true;

		public GameObject referenceMesh;
		public LinkClip[] sequence;
		public string boneRoot = "bn_COG_Root";

		[System.Serializable]
		public class LinkClip {
			public AnimationClip clip;
			public Vector3 velocity;
			public int loopCount = 1;

			public string name {
				get {
					return clip != null ? clip.name : "";
				}
			}
		}

		static Transform SearchRec (Transform tr, string name) {
            return default;
        }

        public void CalculateOffsets(List<Vector3> trace, out Vector3 endPosition)
        {
            endPosition = default(Vector3);
        }

        public override void DrawGizmos()
        {
        }
    }
}

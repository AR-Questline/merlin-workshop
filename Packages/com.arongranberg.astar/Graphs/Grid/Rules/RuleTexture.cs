using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Pathfinding.Graphs.Grid.Rules {
	using Pathfinding.Jobs;

	/// <summary>
	/// Modifies nodes based on the contents of a texture.
	///
	/// This can be used to "paint" penalties or walkability using an external program such as Photoshop.
	///
	/// [Open online documentation to see images]
	///
	/// This rule will pick up changes made to the texture during runtime, assuming the <code> Texture.imageContentsHash </code> property is changed.
	/// This is not always done automatically, so you may have to e.g. increment that property manually if you are doing changes to the texture via code.
	/// Any changes will be applied when the graph is scanned, or a graph update is performed.
	///
	/// See: grid-rules (view in online documentation for working links)
	/// </summary>
	[Pathfinding.Util.Preserve]
	public class RuleTexture : GridGraphRule {
		public Texture2D texture;

		public ChannelUse[] channels = new ChannelUse[4];
		public float[] channelScales = { 1000, 1000, 1000, 1000 };

		public ScalingMode scalingMode = ScalingMode.StretchToFitGraph;
		public float nodesPerPixel = 1;

		NativeArray<int> colors;

		public enum ScalingMode {
			FixedScale,
			StretchToFitGraph,
		}

		public override int Hash {
			get {
				var h = base.Hash ^ (texture != null ? (31 * texture.GetInstanceID()) ^ (int)texture.updateCount : 0);
#if UNITY_EDITOR
				if (texture != null) h ^= (int)texture.imageContentsHash.GetHashCode();
#endif
				return h;
			}
		}

		public enum ChannelUse {
			None,
			/// <summary>Penalty goes from 0 to channelScale depending on the channel value</summary>
			Penalty,
			/// <summary>Node Y coordinate goes from 0 to channelScale depending on the channel value</summary>
			Position,
			/// <summary>If channel value is zero the node is made unwalkable, penalty goes from 0 to channelScale depending on the channel value</summary>
			WalkablePenalty,
			/// <summary>If channel value is zero the node is made unwalkable</summary>
			Walkable,
		}

		public override void Register (GridGraphRules rules)
        {
        }

        public override void DisposeUnmanagedData()
        {
        }

        [BurstCompile]
        public struct JobTexturePosition : IJob, GridIterationUtilities.INodeModifier
        {
            [ReadOnly]
            public NativeArray<int> colorData;
            [WriteOnly]
            public NativeArray<Vector3> nodePositions;
            [ReadOnly]
            public NativeArray<float4> nodeNormals;

            public Matrix4x4 graphToWorld;
            public IntBounds bounds;
            public int2 colorDataSize;
            public float2 scale;
            public float4 channelPositionScale;

            public void ModifyNode(int dataIndex, int dataX, int dataLayer, int dataZ)
            {
            }

            public void Execute()
            {
            }
        }

        [BurstCompile]
        public struct JobTexturePenalty : IJob, GridIterationUtilities.INodeModifier
        {
            [ReadOnly]
            public NativeArray<int> colorData;
            public NativeArray<uint> penalty;
            public NativeArray<bool> walkable;
            [ReadOnly]
            public NativeArray<float4> nodeNormals;

            public IntBounds bounds;
            public int2 colorDataSize;
            public float2 scale;
            public float4 channelPenalties;
            public bool4 channelDeterminesWalkability;

            public void ModifyNode(int dataIndex, int dataX, int dataLayer, int dataZ)
            {
            }

            public void Execute()
            {
            }
        }
	}
}

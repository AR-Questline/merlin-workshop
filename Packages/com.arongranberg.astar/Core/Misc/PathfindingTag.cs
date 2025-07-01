using Pathfinding;

namespace Pathfinding {
	/// <summary>
	/// Represents a single pathfinding tag.
	///
	/// Note: The tag refers to a pathfinding tag, not a unity tag that is applied to GameObjects, or any other kind of tag.
	///
	/// See: tags (view in online documentation for working links)
	/// </summary>
	[System.Serializable]
	public struct PathfindingTag {
		/// <summary>
		/// Underlaying tag value.
		/// Should always be between 0 and <see cref="GraphNode.MaxTagIndex"/> (inclusive).
		/// </summary>
		public uint value;

		public PathfindingTag(uint value) : this()
        {
        }

        public static implicit operator uint (PathfindingTag tag) {
            return default;
        }

        public static implicit operator PathfindingTag(uint tag)
        {
            return default;
        }

        /// <summary>Get the value of the PathfindingTag with the given name</summary>
        public static PathfindingTag FromName(string tagName)
        {
            return default;
        }

        public override string ToString()
        {
            return default;
        }
    }
}

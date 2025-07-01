using System;
using Unity.Entities;

namespace Unity.Rendering
{
    /// <summary>
    /// Frozen scene tag.
    /// </summary>
    [Serializable]
    public struct FrozenRenderSceneTag : ISharedComponentData, IEquatable<FrozenRenderSceneTag>
    {
        /// <summary>
        /// Scene ID.
        /// </summary>
        public Hash128          SceneGUID;

        /// <summary>
        /// Section ID.
        /// </summary>
        public int              SectionIndex;

        /// <summary>
        /// Streaming LOD flags.
        /// </summary>
        public int              HasStreamedLOD;

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="other">Other instance</param>
        /// <returns>True if the objects belong to the same scene and section</returns>
        public bool Equals(FrozenRenderSceneTag other)
        {
            return SceneGUID == other.SceneGUID && SectionIndex == other.SectionIndex;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return SceneGUID.GetHashCode() ^ SectionIndex;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"GUID: {SceneGUID} section: {SectionIndex}";
        }
    }
}

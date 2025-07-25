using System;
using UnityEngine;

namespace Unity.Entities
{
    public struct EditorRenderData : ISharedComponentData, IEquatable<EditorRenderData>
    {
        public ulong SceneCullingMask;
        public bool Equals(EditorRenderData other) => SceneCullingMask == other.SceneCullingMask;
        public override int GetHashCode() => SceneCullingMask.GetHashCode();
    }
}

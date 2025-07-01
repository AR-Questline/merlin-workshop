#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using System.Collections.Generic;
using Unity.Rendering.Occlusion.Masked;


namespace Unity.Rendering.Occlusion
{
    struct Compare : IComparer<ClippedOccluder>
    {
        int IComparer<ClippedOccluder>.Compare(ClippedOccluder x, ClippedOccluder y)
        {
            return x.screenMin.z.CompareTo(y.screenMin.z);
        }
    }
}

#endif

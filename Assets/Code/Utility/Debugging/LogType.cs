using System;
using JetBrains.Annotations;

namespace Awaken.Utility.Debugging {
    [Flags]
    public enum LogType : byte {
        Never = 0,
        
        Minor = 1 << 0,
        Important = 1 << 1,
        Marking = 1 << 2,
        Critical = 1 << 3,
        
        Debug = 1 << 6,
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve] // just for better drawing in the inspector
        Build = Important | Marking | Critical,
        All = byte.MaxValue,
    }
}
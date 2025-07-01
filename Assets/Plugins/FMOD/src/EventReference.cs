using System;

namespace FMODUnity
{
    [Serializable]
    public struct EventReference
    {
        public FMOD.GUID Guid;
        
#if UNITY_EDITOR
        public string Path;
#endif
        
#if UNITY_EDITOR
        public bool IsNull => string.IsNullOrEmpty(Path) && Guid.IsNull;
#else
        public bool IsNull => Guid.IsNull;
#endif
    }
}
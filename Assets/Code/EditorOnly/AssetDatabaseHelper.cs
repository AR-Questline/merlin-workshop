#if UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEditor;

namespace Awaken.TG.EditorOnly {
    public static class AssetDatabaseHelper {
        public static Guid ToSystemGuid(in this GUID unityGuid) {
            return Guid.Parse(unityGuid.ToString());
        }
        
        public static GUID ToUnityGuid(in this Guid systemGuid) {
            return new GUID(systemGuid.ToString("N"));
        }
    }
}
#endif
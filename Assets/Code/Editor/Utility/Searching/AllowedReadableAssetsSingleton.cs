using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Searching {
    public abstract class AllowedReadableAssetsSingleton<T> : ScriptableObject {
        public List<T> values; 
    }
}
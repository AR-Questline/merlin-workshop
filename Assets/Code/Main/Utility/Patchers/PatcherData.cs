using System;

namespace Awaken.TG.Main.Utility.Patchers {
    public class PatcherData {
        [UnityEngine.Scripting.Preserve] public Version version;
        public object data;
        [UnityEngine.Scripting.Preserve] public T Get<T>() => (T) data;

        public PatcherData(Version version, object data) {
            this.version = version;
            this.data = data;
        }
    }
}
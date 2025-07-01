using System;
using System.Diagnostics;

namespace Awaken.TG.Main.Stories.Actors {
    [Conditional("UNITY_EDITOR")]
    public class ActorRefAttribute : Attribute {
        public string propertyName;

        public ActorRefAttribute(string propertyName) {
            this.propertyName = propertyName;
        }
    }
}
using System;
using System.Diagnostics;

namespace Awaken.TG.Main.Heroes.Skills.Graphs.Utils {
    [Conditional("UNITY_EDITOR")]
    public class StringPopupAttribute : Attribute {
        public string SourceFiledName { get; private set; }

        public StringPopupAttribute(string sourceFiledName) {
            SourceFiledName = sourceFiledName;
        }
    }
}
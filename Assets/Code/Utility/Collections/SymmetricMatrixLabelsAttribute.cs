using System;
using System.Diagnostics;

namespace Awaken.TG.Utility.Collections {
    [Conditional("UNITY_EDITOR")]
    public class SymmetricMatrixLabelsAttribute : Attribute {
        public string Provider { get; }

        public SymmetricMatrixLabelsAttribute(string provider) {
            Provider = provider;
        }
    }
}
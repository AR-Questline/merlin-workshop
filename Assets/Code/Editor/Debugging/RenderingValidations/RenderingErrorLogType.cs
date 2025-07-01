using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    public enum RenderingErrorLogType {
        Log = 0,
        Assert = 10,
        Warning = 100,
        Error = 1_000,
        Exception = 10_000,
    }

    public static class RenderingErrorLogTypeExtension {
        public static int Value(this RenderingErrorLogType logType) {
            return (int)logType;
        }
        
        public static string ToHexColor(this RenderingErrorLogType logType) {
            return (logType switch {
                RenderingErrorLogType.Exception => new Color(0.65f, 0.1f, 0.1f),
                RenderingErrorLogType.Error     => new Color(1f, 0.5f, 0f),
                RenderingErrorLogType.Warning   => Color.yellow,
                RenderingErrorLogType.Assert    => Color.cyan,
                _                               => Color.white
            }).ToHex();
        }
    }
}

namespace Awaken.TG.Main.ActionLogs {
    /// <summary>
    /// Glue between ActionLog and log processors
    /// </summary>
    public class LogDisplaySettings {
        // === Fields
        readonly ActionLogDisplayTarget _target;
        readonly object _payload;

        // === Creation

        [UnityEngine.Scripting.Preserve]
        public static LogDisplaySettings GlobalHUD(byte priority) => new(priority, ActionLogDisplayTarget.HUDFloatText);

        [UnityEngine.Scripting.Preserve]
        public static LogDisplaySettings Notification(object payload = null) {
            return new LogDisplaySettings(payload, ActionLogDisplayTarget.Notification);
        }

        LogDisplaySettings(object payload, ActionLogDisplayTarget target) {
            this._target = target;
            this._payload = payload;
        }

        // Public interface
        public void Call(ARLog log) {
            _target?.showLog(log, _payload);
        }
    }
}
using System;
using System.Collections.Generic;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.ActionLogs {
    public partial class ActionLog : Model {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

#if UNITY_EDITOR
        // In editor there are no limits (excepts ram but you know)
        public const int LogsLimit = 65536;
#else
        public const int LogsLimit = 30;
#endif
        // === Fields
        List<ARLog> _logHistory = new List<ARLog>(LogsLimit);
        bool _silent = false;

        // === Events

        public new class Events {
            public static readonly Event<ActionLog, ARLog> LogAdded = new(nameof(LogAdded));
        }

        // === Public interface

        public void Announce(string info, IModel relatedModel, params LogDisplaySettings[] settings) {
            while (_logHistory.Count >= LogsLimit) {
                _logHistory.RemoveAt(0);
            }
            ARLog log = new ARLog(info, relatedModel);
            _logHistory.Add(log);

            if (_silent) return;
            foreach (var setting in settings) {
                setting.Call(log);
            }

            this.Trigger(Events.LogAdded, log);
        }

        [UnityEngine.Scripting.Preserve]
        public IEnumerable<ARLog> History() {
            foreach (ARLog log in _logHistory) {
                yield return log;
            }
        }

        public void DisableDisplay() {
            _silent = true;
        }

        public void EnableDisplay() {
            _silent = false;
        }

        public LogSilenceMode SilenceMode => new(this);
    }
    
    public class LogSilenceMode : IDisposable {
        ActionLog _log;
            
        public LogSilenceMode(ActionLog log) {
            _log = log;
            _log.DisableDisplay();
        }

        public void Dispose() {
            _log.EnableDisplay();
        }
    }
}

using Awaken.TG.MVC;

namespace Awaken.TG.Main.ActionLogs {
    public class LogConfig {
        public string text;
        public IModel relatedModel;
        public LogDisplaySettings displaySettings;
        
        [UnityEngine.Scripting.Preserve]
        public static LogConfig Create(string text, IModel relatedModel, LogDisplaySettings settings) => new LogConfig(text, relatedModel, settings);
        
        [UnityEngine.Scripting.Preserve]
        public LogConfig(string text, IModel relatedModel, LogDisplaySettings displaySettings) {
            this.text = text;
            this.relatedModel = relatedModel;
            this.displaySettings = displaySettings;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void Announce() {
            World.Only<ActionLog>().Announce(text, relatedModel, displaySettings);
        }
    }
}
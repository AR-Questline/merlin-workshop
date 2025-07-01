using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Newtonsoft.Json;

namespace Awaken.TG.Main.ActionLogs {
    public class ARLog {
        public string Content { get; private set; }
        public WeakModelRef<Model> RelatedModel { [UnityEngine.Scripting.Preserve] get; private set; }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        ARLog() { }

        public ARLog(string content, IModel relatedModel = null) {
            Content = content;
            RelatedModel = new WeakModelRef<Model>(relatedModel?.ID);
        }
    }
}

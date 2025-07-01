using System.Collections.Generic;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.UITooltips {
    /// <summary>
    /// TextMeshPro accepts only tooltips that are smaller than given number of characters.
    /// Because it's impossible to fit all tooltips in that range, there needs to be some system of mapping ID -> text. 
    /// </summary>
    public class UITooltipStorage : IService {
        
        Dictionary<string, string> _tooltipsByID = new Dictionary<string, string>();

        public static string ConstructID(IModel model, string id) {
            return $"{model.ID}:{id}";
        }

        public string Get(string id, string fallback = "") {
            _tooltipsByID.TryGetValue(id, out string tooltip);
            return tooltip ?? fallback;
        }

        public void Register(string id, string text, IModel owner = null) {
            owner?.ListenTo(Model.Events.BeforeDiscarded, () => _tooltipsByID.Remove(id), this);
            _tooltipsByID[id] = text;
        }

        public string Register(IModel target, string id, string text, IModel owner = null) {
            id = ConstructID(target, id); 
            Register(id, text, owner);
            return id;
        }
    }
}
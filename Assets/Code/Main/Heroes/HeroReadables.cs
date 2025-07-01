using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroReadables : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroReadables;

        [Saved] HashSet<ItemTemplate> _readablesRead = new();

        public void RegisterTemplateAsRead(ItemTemplate template) {
            _readablesRead.Add(template);
        }

        public bool WasTemplateRead(ItemTemplate template) {
            return _readablesRead.Contains(template);
        }

        [UnityEngine.Scripting.Preserve]
        public void ClearReadTemplates() {
            _readablesRead.Clear();
        }
    }
}
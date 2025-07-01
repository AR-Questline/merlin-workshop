using System.Collections.Generic;

namespace Awaken.TG.Editor.Utility.RichLabels.Configs {
    public class SimpleConfig : RichLabelConfig {
        public override IEnumerable<RichLabelCategory> GetPossibleCategories() => RichLabelCategories;
    }
}

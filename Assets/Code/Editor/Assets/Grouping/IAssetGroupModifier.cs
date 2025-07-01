using System.Collections.Generic;

namespace Awaken.TG.Editor.Assets.Grouping {
    public interface IAssetGroupModifier {
        void Modify(ARAddressableManager manager);
    }
}
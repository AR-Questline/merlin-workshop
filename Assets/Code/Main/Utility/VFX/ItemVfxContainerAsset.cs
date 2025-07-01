using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public class ItemVfxContainerAsset : ScriptableObject, IContainerAsset<ItemVfxContainer> {
        public ItemVfxContainer Container => vfxContainer;
        public ItemVfxContainer vfxContainer;
    }
}
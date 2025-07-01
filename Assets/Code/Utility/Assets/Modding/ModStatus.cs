using Awaken.TG.Assets.Modding;

namespace Awaken.Utility.Assets.Modding {
    public struct ModStatus {
        public readonly int index;
        public bool active;
        
        public ModStatus(int index, bool active = true) {
            this.index = index;
            this.active = active;
        }
        
        
        public readonly ref readonly Mod Data(ModManager manager) {
            return ref manager.AllMods[index];
        }
    }
}
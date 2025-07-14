namespace Awaken.Utility.Assets.Modding {
    public struct ModHandle {
        public readonly int index;
        public bool active;
        
        public ModHandle(int index, bool active = true) {
            this.index = index;
            this.active = active;
        }
    }
}
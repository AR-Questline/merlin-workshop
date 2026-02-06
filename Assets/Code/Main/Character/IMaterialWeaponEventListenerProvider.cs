namespace Awaken.TG.Main.Character {
    public interface IMaterialWeaponEventListenerProvider {
        public int MaterialIndex { get; }
        public string Parameter { get; }
        public float ValueActivated { get; }
        public float ValueDeactivated { get; }
        public float LerpTime { get; }
    }
}

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public interface ITempInteraction : INpcInteraction {
        public bool FastStart { get; }
        public void UnityUpdate();
    }
}
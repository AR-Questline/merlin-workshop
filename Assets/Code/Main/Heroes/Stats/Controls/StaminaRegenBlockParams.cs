namespace Awaken.TG.Main.Heroes.Stats.Controls {

    public struct StaminaRegenBlockParams {
        public bool Blocked;
        public StaminaRegenBlockType BlockType;
        public StaminaRegenBlockParams(bool blocked, StaminaRegenBlockType blockType) {
            Blocked = blocked;
            BlockType = blockType;
        }
    }
    
    public enum StaminaRegenBlockType {
        Overexertion,
        OnStaminaChange,
        AfterAction,
    }
}
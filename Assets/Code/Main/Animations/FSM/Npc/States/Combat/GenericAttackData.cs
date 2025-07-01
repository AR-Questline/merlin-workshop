namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public struct GenericAttackData {
        public bool canBeExited;
        public bool canUseMovement;
        public bool isLooping;
        
        public static GenericAttackData Default => new GenericAttackData {
            canBeExited = true,
            canUseMovement = true,
            isLooping = false,
        };
    }
}

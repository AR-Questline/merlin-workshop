using Awaken.TG.Main.Grounds;

namespace Awaken.TG.Main.Animations {
    public struct AnimationRiggingData {
        public GroundedPosition lookAt;
        public SpineRotationType spineRotationType;
        
        public float rootRigDesiredWeight;
        public float headRigDesiredWeight;
        public float bodyRigDesiredWeight;
        public float combatRigDesiredWeight;
        public float attackRigDesiredWeight;
        
        public float headTurnSpeed;
        public float bodyTurnSpeed;
        public float rootTurnSpeed;
        public float combatTurnSpeed; 
        public float attackTurnSpeed;
    }
}
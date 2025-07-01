namespace Awaken.TG.Main.Heroes.Animations {
    public interface IAnimationStateListener {
        void OnStateEnter(AnimationEvent animationEvent);
        void OnStateExit(AnimationEvent animationEvent);
    }

    public enum AnimationEvent {
        [UnityEngine.Scripting.Preserve] PetTheDog = 0,
        [UnityEngine.Scripting.Preserve] AttackingEnded = 1,
    }
}

using System;
using Animancer;
using Awaken.TG.Main.Character;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Animations.FSM.Shared {
    public abstract partial class ARAnimatorState<T1, T2> : Element<T2> where T1 : ICharacter where T2 : ARAnimatorSubstateMachine<T1> {
        // === Properties & Fields
        public virtual bool CanReEnter => false;
        public virtual bool CanPerformNewAction => true;
        public bool Entered { get; protected set; }
        public virtual float EntryTransitionDuration => 0.25f;

        // === Public API
        public abstract void Enter(float previousStateNormalizedTime, float? overrideCrossFadeTime, Action<ITransition> onNodeLoaded = null);

        public void Update(float deltaTime) {
            if (!Entered) {
                return;
            }
            
            OnUpdate(deltaTime);
        }
        
        public virtual void Exit(bool restarted = false) {
            OnExit(restarted);
        }
        
        // === Helpers
        
        // --- This is used when canceling bow draw animation, so we can play CancelDraw inversely proportional to BowDraw animation.
        protected virtual float OffsetNormalizedTime(float previousNormalizedTime) => 0;
        
        // === Internal Updates
        protected virtual void AfterEnter(float previousStateNormalizedTime) {}
        protected virtual void OnUpdate(float deltaTime) {}
        protected virtual void OnExit(bool restarted) {}
    }
}
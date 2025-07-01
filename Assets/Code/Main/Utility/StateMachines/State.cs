using System;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.StateMachines {
    public abstract class State : IState {
        public StateMachine GenericParent { get; private set; }
        StateMachine IState.GenericParent {
            get => this.GenericParent;
            set => this.GenericParent = value;
        }

        int IState.Index { get; set; }
        public bool Entered { get; private set; }

        public virtual void Init() { }
        
        public void Enter() {
            if (Entered) return;
            Entered = true;
            OnEnter();
        }
        protected abstract void OnEnter();

        public void Exit() {
            if (!Entered) return;
            try {
                World.EventSystem.RemoveAllListenersOwnedBy(this);
                OnExit();
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                Entered = false;
            }

        }
        protected abstract void OnExit();

        public abstract void Update(float deltaTime);
    }

    public abstract class State<T> : State, IState<T> where T : StateMachine {
        public T Parent => (T) GenericParent;
    }

    public class EmptyState<T> : State<T> where T : StateMachine {
        protected override void OnEnter() { }
        protected override void OnExit() { }
        public override void Update(float deltaTime) { }
    }
}
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Utility.StateMachines {
    public interface IState : IListenerOwner {
        [UnityEngine.Scripting.Preserve] StateMachine GenericParent { get; internal set; }
        internal int Index { get; set; }
        bool Entered { get; }

        void Init();
        void Enter();
        void Exit();
        void Update(float deltaTime);
    }

    public interface IState<out T> : IState where T : StateMachine {
        T Parent { get; }
    }
}
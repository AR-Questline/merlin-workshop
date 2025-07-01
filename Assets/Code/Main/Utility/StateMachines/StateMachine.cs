using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.StateMachines {
    public abstract class StateMachine : State {
        Unit _anyUnit;
        Unit[] _units;
        int _current;
        IState _overridenInitialState;

        ref Unit CurrentUnit => ref _units[_current];
        public IState CurrentState => _current < 0 ? null : CurrentUnit.state;

        protected abstract IEnumerable<IState> States();
        protected virtual IState InitialState => _units[0].state;
        
        protected abstract IEnumerable<StateTransition> Transitions();

        public override void Init() {
            BeforeInit();
            
            _units = States().Select(s => new Unit(s)).ToArray();
            for (int i = 0; i <_units.Length; i++) {
                _units[i].state.GenericParent = this;
                _units[i].state.Index = i;
                _units[i].state.Init();
            }

            foreach (StateTransition t in Transitions()) {
                t.transition.destination = t.destination;
                t.transition.childDestination = t.childDestination;
                if (IsMine(t.destination)) {
                    if (t.source == null) {
                        _anyUnit.AddTransition(t.transition);
                    } else if (IsMine(t.source)) {
                        _units[t.source.Index].AddTransition(t.transition);
                    } else {
                        Log.Important?.Error("Trying to create transition whose source doesn't belong to this StateMachine,\nsource: " + t.source + "\ndestination: " + t.destination);
                    }
                } else {
                    Log.Important?.Error("Trying to create transition whose destination doesn't belong to this StateMachine,\nStateMachine: " + this + "\ndestination: " + t.destination);
                }
            }
            
            OnInit();
        }

        protected virtual void BeforeInit(){}
        protected virtual void OnInit(){}

        protected override void OnEnter() {
            foreach (var transition in _anyUnit.ListenTransitions) {
                transition.Listen(this, this);
            }
            _current = _overridenInitialState?.Index ?? InitialState.Index;
            CurrentUnit.state.Enter();
            foreach (var transition in CurrentUnit.ListenTransitions) {
                transition.Listen(this, CurrentUnit.state);
            }
            OnStateChanged(null, CurrentState);
        }
        
        protected override void OnExit() {
            IState previous = CurrentState;
            previous.Exit();
            _current = -1;
            OnStateChanged(previous, null);
        }

        public override void Update(float deltaTime) {
            foreach (var transition in _anyUnit.PollTransitions) {
                try {
                    transition.Poll(this);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
                if (!Entered) {
                    return;
                }
            }

            foreach (var transition in CurrentUnit.PollTransitions) {
                try {
                    transition.Poll(this);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
                if (!Entered) {
                    return;
                }
            }
            
            try {
                CurrentUnit.state.Update(deltaTime);
                OnUpdate(deltaTime);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
        protected virtual void OnUpdate(float deltaTime) { }

        void ChangeState(IState state, IState childState) {
            if (state == CurrentState) {
                return;
            }

            StateMachine stateMachine = state as StateMachine;
            if (stateMachine != null) {
                stateMachine._overridenInitialState = childState;
            }
            
            IState previous = CurrentState;
            CurrentState.Exit();
            _current = state.Index;
            CurrentState.Enter();
            foreach (var transition in CurrentUnit.ListenTransitions) {
                transition.Listen(this, CurrentUnit.state);
            }
            OnStateChanged(previous, CurrentState);
            
            if (stateMachine != null) {
                stateMachine._overridenInitialState = null;
            }
        }

        protected virtual void OnStateChanged(IState previous, IState current) { }

        public bool IsMine(IState state) {
            return state != null && _units[state.Index].state == state;
        }

        protected readonly struct StateTransition {
            public readonly IState source;
            public readonly IState destination;
            public readonly IState childDestination;
            public readonly Transition transition;

            public StateTransition(IState source, (IState destination, IState childDestination) destination, Transition transition) {
                this.source = source;
                this.destination = destination.destination;
                this.childDestination = destination.childDestination;
                this.transition = transition;
            }
            
            public StateTransition(IState source, IState destination, Transition transition) {
                this.source = source;
                this.destination = destination;
                this.childDestination = null;
                this.transition = transition;
            }
        }
        
        protected abstract class Transition {
            public IState destination;
            public IState childDestination;
        }

        protected class PollTransition : Transition {
            Func<bool> _predicate;

            public PollTransition(Func<bool> predicate) {
                _predicate = predicate;
            }

            public void Poll(StateMachine machine) {
                if (_predicate()) {
                    machine.ChangeState(destination, childDestination);
                }
            }
        }
        protected abstract class ListenTransition : Transition {
            public abstract IEventListener Listen(StateMachine machine, IListenerOwner owner);
            public static ListenTransition<TSource, TPayload> New<TSource, TPayload>(TSource model, Event<TSource, TPayload> evt) where TSource : class, IModel {
                return new(model, evt);
            }

            public static ListenTransition<TSource, TPayload> New<TSource, TPayload>(TSource model, Event<TSource, TPayload> evt,
                Func<TPayload, bool> condition) where TSource : class, IModel {
                return new(model, evt, condition);
            }
        }
        protected class ListenTransition<TSource, TPayload> : ListenTransition where TSource : class, IModel {
            TSource _model;
            Event<TSource, TPayload> _evt;
            Func<TPayload, bool> _condition;
            
            StateMachine _machine;

            public ListenTransition(TSource model, Event<TSource, TPayload> evt) {
                _model = model;
                _evt = evt;
            }
            public ListenTransition(TSource model, Event<TSource, TPayload> evt, Func<TPayload, bool> condition) {
                _model = model;
                _evt = evt;
                _condition = condition;
            }
            

            public override IEventListener Listen(StateMachine machine, IListenerOwner owner) {
                _machine = machine;
                return _model.ListenTo(_evt, Trigger, owner);
            }

            void Trigger(TPayload payload) {
                if (_condition == null || _condition(payload)) {
                    _machine.ChangeState(destination, childDestination);
                }
            }
        }

        struct Unit {
            public IState state;
            PollTransition[] _pollTransitions;
            ListenTransition[] _listenTransitions;

            public Unit(IState state) : this() {
                this.state = state;
            }

            public void AddTransition(Transition transition) {
                if (transition is PollTransition poll) {
                    AddPollTransition(poll);
                } else if (transition is ListenTransition listenTransition) {
                    AddListenTransition(listenTransition);
                }
            }
            void AddPollTransition(PollTransition transition) {
                if (_pollTransitions == null) {
                    _pollTransitions = new PollTransition[1];
                } else {
                    Array.Resize(ref _pollTransitions, _pollTransitions.Length + 1);
                }

                _pollTransitions[^1] = transition;
            }

            void AddListenTransition(ListenTransition transition) {
                if (_listenTransitions == null) {
                    _listenTransitions = new ListenTransition[1];
                } else {
                    Array.Resize(ref _listenTransitions, _listenTransitions.Length + 1);
                }

                _listenTransitions[^1] = transition;
            }

            public PollTransition[] PollTransitions => _pollTransitions ?? Array.Empty<PollTransition>();
            public ListenTransition[] ListenTransitions => _listenTransitions ?? Array.Empty<ListenTransition>();
        }
    }

    public abstract class StateMachine<T> : StateMachine, IState<T> where T : StateMachine {
        public T Parent => (T) GenericParent;
    }
}
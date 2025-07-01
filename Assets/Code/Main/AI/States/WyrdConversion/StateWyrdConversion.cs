using System;
using System.Collections.Generic;
using Awaken.TG.Main.Utility.StateMachines;

namespace Awaken.TG.Main.AI.States.WyrdConversion {
    public class StateWyrdConversion : NpcStateMachine<StateAIWorking> {
        public StateWyrdConversionIn WyrdConversionIn { get;} = new ();
        public StateWyrdConversionOut WyrdConversionOut { get;} = new ();

        protected override IEnumerable<IState> States() {
            yield return WyrdConversionIn;
            yield return WyrdConversionOut;
        }

        protected override IEnumerable<StateTransition> Transitions() {
            return Array.Empty<StateTransition>();
        }

        public bool ConversionEnded() {
            return (CurrentState as StateWyrdConversionBase)?.ConversionEnded ?? true;
        }
    }
}
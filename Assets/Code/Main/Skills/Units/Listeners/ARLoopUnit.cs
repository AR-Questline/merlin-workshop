using System;
using System.Collections;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Listeners {
    public abstract class ARLoopUnit : ARUnit {
        protected abstract IEnumerable Collection(Flow flow);
        protected abstract ValueOutput Payload();
        protected ControlInput _enter;
        protected ControlOutput _exit;
        protected ControlOutput _body;
        protected ValueOutput _payload;

        protected override void Definition() {
            _enter = ControlInput("enter", Loop);
            _exit = ControlOutput("exit");
            _body = ControlOutput("body");
            _payload = Payload();

            Succession(_enter, _body);
            Succession(_enter, _exit);
        }

        int Start(Flow flow, IEnumerable collection, out IEnumerator enumerator, out int currentIndex) {
            enumerator = collection.GetEnumerator();
            currentIndex = -1;
            return flow.EnterLoop();
        }

        bool MoveNext(Flow flow, IEnumerator enumerator, ref int currentIndex) {
            var result = enumerator.MoveNext();
            if (result) {
                flow.SetValue(_payload, enumerator.Current);
                currentIndex++;
            }
            return result;
        }
        
        protected ControlOutput Loop(Flow flow) {
            var loop = Start(flow, Collection(flow), out var enumerator, out var currentIndex);
            var stack = flow.PreserveStack();

            try {
                while (flow.LoopIsNotBroken(loop) && MoveNext(flow, enumerator, ref currentIndex)) {
                    flow.Invoke(_body);
                    flow.RestoreStack(stack);
                }
            } finally {
                (enumerator as IDisposable)?.Dispose();
            }
            flow.DisposePreservedStack(stack);
            flow.ExitLoop(loop);
            return _exit;
        }
    }
}
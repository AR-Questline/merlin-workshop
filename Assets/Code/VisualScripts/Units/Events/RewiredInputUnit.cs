using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Events {
    [UnityEngine.Scripting.Preserve]
    public partial class RewiredInputUnit : ARUnit, IEventUnit, IGraphElementWithData {
        [DoNotSerialize] ValueInput KeyBind { get; set; }
        ControlOutput _trigger;
        public override bool isControlRoot => true;
        public bool coroutine => false;
        
        protected override void Definition() {
            KeyBind = ValueInput(nameof(KeyBind), string.Empty);
            _trigger = ControlOutput("trigger");
        }

        public void StartListening(GraphStack stack) {
            using var flow = Flow.New(stack.AsReference());
            var keyBind = flow.GetValue<string>(this.KeyBind);
            var data = stack.GetElementData<Data>(this);
            data.mapUnitHandlerSource = new RewiredInputUnitHandler(stack.AsReference(), keyBind, _trigger);
            World.Only<GameUI>().AddElement(data.mapUnitHandlerSource);
        }

        public void StopListening(GraphStack stack) {
            var data = stack.GetElementData<Data>(this);
            if (data.mapUnitHandlerSource != null) {
                data.mapUnitHandlerSource.Discard();
                data.mapUnitHandlerSource = null;
            }
        }

        public bool IsListening(GraphPointer pointer) {
            return pointer.GetElementData<Data>(this).mapUnitHandlerSource != null;
        }

        public IGraphElementData CreateData() {
            return new Data();
        }

        class Data : IGraphElementData {
            public RewiredInputUnitHandler mapUnitHandlerSource;
        }

        public partial class RewiredInputUnitHandler : Element<GameUI>, IUIHandlerSource, IUIAware {
            public sealed override bool IsNotSaved => true;

            public UIContext Context => UIContext.All;
            public int Priority => 0;
            ControlOutput _controlOutput;
            GraphReference _graphReference;
            string _keyBind;

            public RewiredInputUnitHandler(GraphReference graphReference, string keyBind, ControlOutput controlOutput) {
                _controlOutput = controlOutput;
                _graphReference = graphReference;
                _keyBind = keyBind;
            }
            public void ProvideHandlers(UIPosition _, List<IUIAware> handlers) {
                if (UIStateStack.Instance.State.IsMapInteractive) {
                    handlers.Add(this);
                }
            }

            public UIResult Handle(UIEvent evt) {
                if (evt is UIKeyDownAction action) {
                    if (action.Name == _keyBind) {
                        SafeGraph.Run(AutoDisposableFlow.New(_graphReference), _controlOutput);
                        return UIResult.Accept;
                    }
                }
                
                return UIResult.Ignore;
            }
        }
    }
}
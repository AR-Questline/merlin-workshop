using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Locations {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(Update))]
    [UnityEngine.Scripting.Preserve]
    public partial class LocationUpdateUnit : ARUnit, IGraphElementWithData {
        ControlOutput _update;
        ValueOutput _deltaTime;
        ValueOutput _updateModel;
        
        protected override void Definition() {
            ARValueInput<Location> inLocation = RequiredARValueInput<Location>("location");
            
            var start = ControlInput("enter", flow => {
                StartUpdate(flow, inLocation.Value(flow));
                return null;
            });
            ControlInput("stop", StopUpdate);

            _update = ControlOutput("update");
            _deltaTime = ValueOutput<float>("deltaTime");
            _updateModel = ValueOutput<Model>("model");
            Succession(start, _update);
        }
        
        void StartUpdate(Flow flow, Location location) {
            var reference = flow.stack.AsReference();

            UpdateVS updateVS = null;
            updateVS = new UpdateVS(deltaTime => {
                var f = AutoDisposableFlow.New(reference);
                f.flow.SetValue(_deltaTime, deltaTime);
                f.flow.SetValue(_updateModel, updateVS);
                SafeGraph.Run(f, _update);
            });
            
            flow.SetValue(_updateModel, updateVS);
            
            var effect = location.AddElement(updateVS);
            flow.stack.GetElementData<Data>(this).effect = effect;
        }

        ControlOutput StopUpdate(Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            if (data.effect == null) return null;
            data.effect.Discard();
            data.effect = null;
            return null;
        }

        public IGraphElementData CreateData() {
            return new Data();
        }

        public partial class UpdateVS : Element<Location> {
            public sealed override bool IsNotSaved => true;

            readonly TimeDependent.Update _action;

            public UpdateVS(TimeDependent.Update action) {
                _action = action;
            }

            protected override void OnInitialize() {
                ParentModel.GetOrCreateTimeDependent().WithUpdate(_action);
            }

            protected override void OnDiscard(bool fromDomainDrop) {
                ParentModel.GetTimeDependent()?.WithoutUpdate(_action);
            }
        }
        
        class Data : IGraphElementData {
            public UpdateVS effect;
        }
    }
}
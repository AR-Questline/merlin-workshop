using Awaken.TG.Main.Skills.Passives;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC.Elements;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(Update))]
    [UnityEngine.Scripting.Preserve]
    public partial class PassiveUpdateUnit : PassiveSpawnerUnit {
        ControlOutput _update;
        ValueOutput _deltaTime;
        
        protected override void Definition() {
            _update = ControlOutput("update");
            _deltaTime = ValueOutput<float>("deltaTime");
        }

        protected override IPassiveEffect Passive(Skill skill, Flow flow) {
            var reference = flow.stack.AsReference();
            return new Effect(deltaTime => {
                var f = AutoDisposableFlow.New(reference);
                f.flow.SetValue(_deltaTime, deltaTime);
                SafeGraph.Run(f, _update);
            });
        }

        public partial class Effect : Element<Skill>, IPassiveEffect {
            public sealed override bool IsNotSaved => true;

            readonly TimeDependent.Update _action;

            public Effect(TimeDependent.Update action) {
                _action = action;
            }

            protected override void OnInitialize() {
                ParentModel.Owner.GetOrCreateTimeDependent().WithUpdate(_action);
            }

            protected override void OnDiscard(bool fromDomainDrop) {
                ParentModel.Owner.GetTimeDependent()?.WithoutUpdate(_action);
            }
        }
    }
}
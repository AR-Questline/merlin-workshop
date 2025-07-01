using Awaken.TG.Main.Rendering;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine.Rendering;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(Volume))]
    public abstract class PassivePostProcessUnit : PassiveUnit {
        ARValueInput<float> _weight;
        ARValueInput<float> _timeIn;
        ARValueInput<float> _timeOut;

        protected override void Definition() {
            _weight = InlineARValueInput(nameof(_weight), 1f);
            _timeIn = InlineARValueInput(nameof(_timeIn), 1f);
            _timeOut = InlineARValueInput(nameof(_timeOut), 1f);
        }

        protected abstract VolumeWrapper Volume { get; }
        
        public override void Enable(Skill skill, Flow flow) {
            Volume?.SetOwnerWeight(skill.ContextID.GetHashCode(), _weight.Value(flow), 1 / _timeIn.Value(flow));
        }

        public override void Disable(Skill skill, Flow flow) {
            Volume?.SetOwnerWeight(skill.ContextID.GetHashCode(), 0, 1 / _timeOut.Value(flow));
        }
    }

    [UnityEngine.Scripting.Preserve]
    public class PassiveSlowmotionVolumeUnit : PassivePostProcessUnit {
        protected override VolumeWrapper Volume => World.Services.TryGet<SpecialPostProcessService>(true)?.VolumeWyrdskillSlomotion;
    }
}
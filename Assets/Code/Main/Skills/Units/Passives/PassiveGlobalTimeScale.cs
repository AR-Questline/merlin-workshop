using System;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(Timer))]
    [UnityEngine.Scripting.Preserve]
    public class PassiveGlobalTimeScale : PassiveUnit {
        const string TimeScaleID = "passive.timescale";
        const float Delay = 0.5f;

        ARValueInput<float> _globalTimeScale;
        ARValueInput<float> _myTimeScale;

        protected override void Definition() {
            _globalTimeScale = InlineARValueInput(nameof(_globalTimeScale), 1f);
            _myTimeScale = InlineARValueInput(nameof(_myTimeScale), 1f);
        }

        public override void Enable(Skill skill, Flow flow) {
            float globalTimeScale = _globalTimeScale.Value(flow);
            float myTimeScale = _myTimeScale.Value(flow);

            // set globalTimeScale to _globalTimeScale value and adjust SkillOwner's TimeScale so it will be _myTimeScale
            string sourceID = SkillContextID(skill);
            AddTimeModifier(World.Only<GlobalTime>(), sourceID, globalTimeScale);
            AddTimeModifier(skill.Owner, sourceID, myTimeScale / globalTimeScale);
        }

        public override void Disable(Skill skill, Flow flow) {
            string sourceID = SkillContextID(skill);
            RemoveTimeModifier(skill.Owner, sourceID);
            RemoveTimeModifier(World.Only<GlobalTime>(), sourceID);
        }

        static string SkillContextID(Skill skill) {
            return TimeScaleID + skill.ContextID;
        }

        static void AddTimeModifier(IModel model, string sourceID, float timeScale) {
            model?.AddTimeModifier(new MultiplyTimeModifier(sourceID, timeScale, Delay));
        }

        static void RemoveTimeModifier(IModel model, string sourceID) {
            model?.RemoveTimeModifiersFor(sourceID);
        }
    }
}
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    public interface IPassiveUnit : ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public PassiveType Type { get; }

        void Enable(Skill skill, Flow flow);
        void Disable(Skill skill, Flow flow);
    }

    public abstract class PassiveUnit : ARUnit, IPassiveUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public PassiveType Type { get; [UnityEngine.Scripting.Preserve] private set; }
        
        public abstract void Enable(Skill skill, Flow flow);
        public abstract void Disable(Skill skill, Flow flow);
        
        public override bool isControlRoot => true;
    }

    public enum PassiveType {
        Learn,
        Equip,
        Submit,
    }
}
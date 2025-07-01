using Awaken.TG.Main.Utility.Skills;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.StateGraphs {
    [Descriptor(typeof(ScriptMachineWithSkill))]
    public class ScriptMachineWithSkillDescriptor : MachineDescriptor<ScriptMachineWithSkill, MachineDescription> {
        public ScriptMachineWithSkillDescriptor(ScriptMachineWithSkill target) : base(target) {}
    }
}

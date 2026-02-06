using Awaken.TG.Main.Utility.Skills;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Awaits {
    public abstract class ARWaitUnit : WaitUnit {
        protected ControlOutput TryExit(Flow flow) {
            if (flow.stack?.machine is ScriptMachineWithSkill { Owner: { HasBeenDiscarded: true } }) {
                return null;
            }
            return exit;
        }
    }
}
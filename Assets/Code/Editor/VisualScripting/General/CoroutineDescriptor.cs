using Awaken.TG.VisualScripts.Units.General;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.General {
    [Descriptor(typeof(CoroutineUnit))]
    public class CoroutineDescriptor : UnitDescriptor<CoroutineUnit> {
        public CoroutineDescriptor(CoroutineUnit target) : base(target) { }

        protected override void DefinedPort(IUnitPort port, UnitPortDescription description) {
            base.DefinedPort(port, description);
            if (port is ControlOutput) {
                description.icon = BoltFlow.Icons.coroutine;
            }
        }
    }
}
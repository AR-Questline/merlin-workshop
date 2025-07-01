using Awaken.TG.VisualScripts.Units.General;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.General {
    [Descriptor(typeof(SafeGetVariable))]
    public class SafeGetVariableDescriptor : UnitDescriptor<SafeGetVariable> {
        public SafeGetVariableDescriptor(SafeGetVariable target) : base(target) { }

        protected override void DefinedPort(IUnitPort port, UnitPortDescription description) {
            if (port is IUnitValuePort) {
                description.showLabel = false;
            }
        }
        
        protected override EditorTexture DefinedIcon() {
            return BoltCore.Icons.VariableKind(unit.kind);
        }
    }
}
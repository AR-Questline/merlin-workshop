using Awaken.TG.Main.Skills.Units.Getters;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.NodeDescriptors {
    [Descriptor(typeof(GetNPCsInChain))]
    public class GetNPCsInChainDescriptor : UnitDescriptor<GetNPCsInChain> {
        public GetNPCsInChainDescriptor(GetNPCsInChain target) : base(target) { }

        protected override void DefinedPort(IUnitPort port, UnitPortDescription description) {
            base.DefinedPort(port, description);
            switch (port.key) {
                case "chainRangeDecreasePerLink":
                    description.summary = "Values 0-1\n0.1 = 10% decrease per link";
                    break;
            }
        }
    }
}
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Skills.Units.Passives;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners {
    
    public class PassiveListener : GraphListener, IPassiveUnit {
        
        [Serialize, Inspectable, UnitHeaderInspectable]
        public PassiveType Type { get; [UnityEngine.Scripting.Preserve] private set; }
        
        public void Enable(Skill skill, Flow flow) {
            StartListening(flow.stack);
        }

        public void Disable(Skill skill, Flow flow) {
            StopListening(flow.stack);
        }
    }
}
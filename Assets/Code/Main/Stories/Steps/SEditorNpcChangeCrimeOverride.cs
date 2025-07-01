using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Change NPC Crimes"), NodeSupportsOdin]
    public class SEditorNpcChangeCrimeOverride : EditorStep {
        public LocationReference locations;
        public bool disableCrimes = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcChangeCrimeOverride {
                locations = locations,
                disableCrimes = disableCrimes
            };
        }
    }

    public partial class SNpcChangeCrimeOverride : StoryStepWithLocationRequirement {
        
        public LocationReference locations;
        public bool disableCrimes = true;

        protected override LocationReference RequiredLocations => locations;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(disableCrimes);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_NpcChangeCrimeOverride;

            [Saved] bool _disableCrimes;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(bool disableCrimes) {
                _disableCrimes = disableCrimes;
            }
            
            public override void Execute(Location location) {
                NoLocationCrimeOverride currentElement = location.TryGetElement<NoLocationCrimeOverride>();
                if (currentElement != null && !_disableCrimes) {
                    location.RemoveElement(currentElement);
                } else if (_disableCrimes && currentElement == null) {
                    location.AddElement(new NoLocationCrimeOverride());
                }
            }
        }
    }
}
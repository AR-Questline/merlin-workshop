using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Spawn VFX"), NodeSupportsOdin]
    public class SEditorTriggerVFXSpawn : EditorStep {
        public LocationReference locationReference;
        [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference vfxAssetRef;
        public float vfxDuration = 5f;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STriggerVFXSpawn {
                locationReference = locationReference,
                vfxAssetRef = vfxAssetRef,
                vfxDuration = vfxDuration,
            };
        }
    }

    public partial class STriggerVFXSpawn : StoryStepWithLocationRequirement {
        public LocationReference locationReference;
        public ShareableARAssetReference vfxAssetRef;
        public float vfxDuration;
        
        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution() {
                vfxAssetRef = vfxAssetRef,
                vfxDuration = vfxDuration,
            };
        }

        public partial class StepExecution : DeferredLocationExecution {
            public ShareableARAssetReference vfxAssetRef;
            public float vfxDuration;

            public override ushort TypeForSerialization => SavedTypes.StepExecution_TriggerVFXSpawn;

            public override void Execute(Location location) {
                PrefabPool.InstantiateAndReturn(vfxAssetRef, location.Coords, location.Rotation, vfxDuration).Forget();
            }
        }
    }
}
using System;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Discard"), NodeSupportsOdin]
     public class SEditorLocationDiscard : EditorStep {
         public LocationReference locationReference;

         protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
             return new SLocationDiscard {
                 locationReference = locationReference 
             };
         }
     }

    public partial class SLocationDiscard : StoryStepWithLocationRequirement {
        public LocationReference locationReference;

        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution();
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_LocationDiscard;
            
            public override void Execute(Location location) {
                DiscardAtEndOfFrame(location).Forget();
            }
             
            async UniTaskVoid DiscardAtEndOfFrame(Location location) {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                try {
                    location.Discard();
                } catch (Exception e) {
                    Log.Important?.Error("Failed to discard matching locations from story! Exception below.");
                    Debug.LogException(e);
                }
            }
        }
    }
}
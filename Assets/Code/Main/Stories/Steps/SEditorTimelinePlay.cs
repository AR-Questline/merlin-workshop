using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Animations;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Timeline: Load and Play"), NodeSupportsOdin]
    public class SEditorTimelinePlay : EditorStep {
        public LocationReference locationReference;
        
        [ARAssetReferenceSettings(new[] { typeof(PlayableAsset) }, true, AddressableGroup.Animations)] 
        public ShareableARAssetReference playableAssetReference;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STimelinePlay {
                locationReference = locationReference,
                playableAssetReference = playableAssetReference
            };
        }
    }
    
    public partial class STimelinePlay : StoryStep {
        public LocationReference locationReference;
        public ShareableARAssetReference playableAssetReference;
        
        public override StepResult Execute(Story story) {
            var stepResult = new StepResult();
            PlayTimeline(story, stepResult).Forget();
            return stepResult;
        }
        
        async UniTaskVoid PlayTimeline(Story api, StepResult result) {
            if (!playableAssetReference.IsSet) {
                return;
            }
            
            List<UniTask> tasks = new();
            
            foreach (var matchingLocation in locationReference.MatchingLocations(api).ToArray()) {
                if (matchingLocation.TryGetElement(out TimelineElement playableDirectorElement)) {
                    var task = playableDirectorElement.PlayTimelineAsset(playableAssetReference);
                    tasks.Add(task);
                }
            }
            
            if (!await AsyncUtil.WaitForAll(api, tasks)) {
                return;
            }
            
            result.Complete();
        }
    }
}
using System.Linq;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Cysharp.Threading.Tasks;
using FMOD;
using FMODUnity;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Audio/Audio: Play SFX"), NodeSupportsOdin]
    public class SEditorPlaySFX : EditorStep {
        public LocationReference locationRef = new() { targetTypes = TargetType.Self };
        public EventReference audioClip;
        public bool waitForEnd;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPlaySFX {
                locationRef = locationRef,
                audioClip = audioClip,
                waitForEnd = waitForEnd
            };
        }
    }

    public partial class SPlaySFX : StoryStep {
        public LocationReference locationRef;
        public EventReference audioClip;
        public bool waitForEnd;

        public bool HasAudioClip => !audioClip.Guid.IsNull && false;//RuntimeManager.StudioSystem.getEventByID(audioClip.Guid, out _) == RESULT.OK;
        
        public override StepResult Execute(Story story) {
            if (HasAudioClip) {
                Location[] locations = locationRef.MatchingLocations(story).ToArray();
                if (locations.Any()) {
                    foreach (var location in locations) {
                        location.LocationView.PlayAudioClip(audioClip, true);                        
                    }
                } else {
                    story.Hero?.NonSpatialVoiceOvers?.PlayOneShot(audioClip);
                }

                if (waitForEnd) {
                    //RuntimeManager.StudioSystem.getEventByID(audioClip.Guid, out var eventDescription);
                    //eventDescription.getLength(out int eventLength);
                    // StepResult result = new();
                    // WaitForEnd(eventLength, result).Forget();
                    // return result;
                }
            }
            return StepResult.Immediate;
        }

        static async UniTaskVoid WaitForEnd(int clipLength, StepResult result) {
            await UniTask.Delay(clipLength);
            result.Complete();
        }
    }
}

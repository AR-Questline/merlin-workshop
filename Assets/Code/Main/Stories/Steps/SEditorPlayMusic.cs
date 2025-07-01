using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;
using AudioType = Awaken.TG.Main.AudioSystem.AudioType;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Audio/Audio: Play Story Music"), NodeSupportsOdin]
    public class SEditorPlayMusic : EditorStep {
        public AudioType managerType;
        public BaseAudioSource toPlay;
        [InfoBox("When not using as one shot you must manually invoke Stop Story Music!", InfoMessageType.Warning, nameof(NotOneShot))]
        public bool asOneShot = true;

        bool NotOneShot => !asOneShot;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPlayMusic {
                managerType = managerType,
                toPlay = toPlay,
                asOneShot = asOneShot
            };
        }
    }

    public partial class SPlayMusic : StoryStep {
        public AudioType managerType;
        public BaseAudioSource toPlay;
        public bool asOneShot = true;
        
        public override StepResult Execute(Story story) {
            World.Add(new StoryMusic(managerType, toPlay, asOneShot));
            return StepResult.Immediate;
        }
    }
}
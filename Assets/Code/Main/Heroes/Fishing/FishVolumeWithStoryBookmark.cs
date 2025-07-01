using Awaken.TG.Main.Stories;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Fishing {
    public class FishVolumeWithStoryBookmark : FishVolume {
        [InfoBox("This Story will be started when player starts fishing (not when catching a fish) in this volume")]
        [SerializeField] StoryBookmark startBookmark;
        [InfoBox("This Story will be started when player catches anything from this volume")]
        [SerializeField] StoryBookmark catchBookmark;
        
        public override void OnGetVolume() {
            if (startBookmark is { IsValid: true }) {
                Story.StartStory(StoryConfig.Base(startBookmark, typeof(VDialogue)));
            }
        }
        
        public void OnFishCaught() {
            if (catchBookmark is { IsValid: true }) {
                Story.StartStory(StoryConfig.Base(catchBookmark, typeof(VDialogue)));
            }
        }
    }
}
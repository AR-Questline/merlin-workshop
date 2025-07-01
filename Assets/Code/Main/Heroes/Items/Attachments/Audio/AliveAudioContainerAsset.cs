using Awaken.TG.Main.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    public class AliveAudioContainerAsset : ScriptableObject, IContainerAsset<AliveAudioContainer> {
        public AliveAudioContainer Container => audioContainer;
        public AliveAudioContainer audioContainer;
    }
}
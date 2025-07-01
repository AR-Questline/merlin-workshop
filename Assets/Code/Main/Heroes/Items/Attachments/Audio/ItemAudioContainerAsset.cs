using Awaken.TG.Main.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    public class ItemAudioContainerAsset : ScriptableObject, IContainerAsset<ItemAudioContainer> {
        public ItemAudioContainer Container => audioContainer;
        public ItemAudioContainer audioContainer;
    }
}
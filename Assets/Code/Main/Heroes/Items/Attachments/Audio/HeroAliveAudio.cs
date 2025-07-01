using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    public partial class HeroAliveAudio : AliveAudio {
        public override ushort TypeForSerialization => SavedModels.HeroAliveAudio;

        [Saved] AliveAudioContainer _audioContainer;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        HeroAliveAudio() {}
        
        public HeroAliveAudio(AliveAudioContainer audioContainerWrapper) {
            _audioContainer = audioContainerWrapper;
        }

        public override void InitFromAttachment(AliveAudioAttachment spec, bool isRestored) {
            Log.Important?.Error($"{nameof(HeroAliveAudio)} does not support attachment initialization.");
        }
        
        public override AliveAudioContainer GetContainer(bool isWyrdConverted) {
            return _audioContainer;
        }
    }
}
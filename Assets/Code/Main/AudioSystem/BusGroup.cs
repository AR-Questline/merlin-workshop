using Awaken.Utility.Enums;
using FMOD;
using FMOD.Studio;
using FMODUnity;

namespace Awaken.TG.Main.AudioSystem {
    public class BusGroup: RichEnum {
        readonly GUID _busGUID;
        
        [UnityEngine.Scripting.Preserve]
        public static readonly BusGroup
            SFX = new(nameof(SFX), GUID.Parse("5d2dbd75-6919-4177-a185-9537ff8a8b45")),
            //VideoCredits = new(nameof(VideoCredits), GUID.Parse("9667c686-54e7-4e26-8ec0-d7e6553af7a3")),
            VoiceOvers = new(nameof(VoiceOvers), GUID.Parse("4e89187e-44d8-4a08-9c9e-2444cce67206")),
            Reverbs = new(nameof(Reverbs), GUID.Parse("95028463-8166-486b-8f8d-01aab5cde0fc"));
        
        BusGroup(string enumName, GUID busGUID) : base(enumName) {
            _busGUID = busGUID;
        }
        
        // public bool TryGetBus(out Bus bus) {
        //     return RuntimeManager.StudioSystem.getBusByID(_busGUID, out bus) == RESULT.OK;
        // }
    }
}
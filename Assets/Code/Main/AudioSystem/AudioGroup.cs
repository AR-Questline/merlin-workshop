// ReSharper disable InconsistentNaming

using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;
using FMOD;
using FMOD.Studio;
using FMODUnity;

namespace Awaken.TG.Main.AudioSystem {
    public class AudioGroup : RichEnum {
        readonly string _settingNameId;
        readonly GUID _vcaGUID;
        
        AudioGroup(string enumName, string settingNameId, GUID vcaGUID) : base(enumName) {
            _settingNameId = settingNameId;
            _vcaGUID = vcaGUID;
        }

        [UnityEngine.Scripting.Preserve]
        public static AudioGroup
            MASTER = new(nameof(MASTER), LocTerms.SettingsMasterVolume, GUID.Parse("e725c771-ec8b-47a4-ac19-8a70a2ed77e0")),
            MUSIC = new(nameof(MUSIC), LocTerms.SettingsMusicVolume, GUID.Parse("ae9c258b-a0ff-41c0-97b8-3950a075bfb7")),
            SFX = new(nameof(SFX), LocTerms.SettingsSFXVolume, GUID.Parse("e27b6971-60d8-486a-a37b-30da450b1a4e")),
            VO = new(nameof(VO), LocTerms.SettingsVoiceVolume, GUID.Parse("e1b8ea4b-05ee-4acd-affb-d6ff682ed0ad")),
            UI = new(nameof(UI), LocTerms.SettingsUIVolume, new GUID()),
            VIDEO = new(nameof(VIDEO), LocTerms.SettingsVideoVolume, GUID.Parse("f9b31234-88de-4f74-a2d8-4d36512eac24"));

        public string SettingName() {
            return _settingNameId.Translate();
        }
        
        // public bool TryGetVCA(out VCA vca) {
        //     return RuntimeManager.StudioSystem.getVCAByID(_vcaGUID, out vca) == RESULT.OK;
        // }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Other {
    public partial class SkinSetting : Setting {
        readonly string _prefId;
        readonly List<ToggleOption> _toggleOptions = new();
        readonly ListDictionary<ToggleOption, Skin> _presetByOption = new();
        readonly Skin[] _skins;

        public override bool IsVisible => HasDlcOrNonDlcRequired;
        public override IEnumerable<PrefOption> Options => EnumOption.Yield();
        public sealed override string SettingName { get; }
        Skin ActiveSkin => _presetByOption[EnumOption.Option];
        bool HasDlcOrNonDlcRequired => _dlcCategory == null || SocialService.Get.HasDlc((DlcCategory)_dlcCategory);
        DlcCategory? _dlcCategory;
        EnumArrowsOption EnumOption { get; }
        
        public SkinSetting(string settingName, string prefId, Skin[] skins, Skin defaultSetting, DlcCategory? dlcCategory = null) {
            _skins = skins;
            _prefId = prefId;
            _dlcCategory = dlcCategory;
            
            foreach (Skin skin in _skins) {
                var option = new ToggleOption(GetPresetId(skin), skin.DisplayName, skin == defaultSetting, true); 
                _toggleOptions.Add(option);
                _presetByOption.Add(option, skin);
            }

            SettingName = settingName;
            ToggleOption defaultOption = _toggleOptions.FirstOrDefault(o => o.DefaultValue);
            EnumOption = new EnumArrowsOption(_prefId, SettingName, defaultOption, true, _toggleOptions.ToArray());
            EnumOption.AddPreview(() => ActiveSkin.Preview, () => ActiveSkin.DisplayName);
            if (!HasDlcOrNonDlcRequired) {
                EnumOption.RestoreDefault();
                EnumOption.Apply();
            }
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, this, Init); //to przez to
        }

        string GetPresetId(Skin preset) {
            return $"{_prefId}_{preset.EnumName}";
        }

        void Init() {
            EnumOption.onChange += _ => SetSkinFlags();
            SetSkinFlags();
        }

        void SetSkinFlags() {
            foreach (Skin skin in _skins) {
                StoryFlags.Set(skin.Flag, skin == ActiveSkin);
            }
        }
    }
}

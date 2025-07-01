using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Settings.Options;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Audio {
    /// <summary>
    /// Manages audio slider for volume settings. Takes care of all the calculations from linear to logarithm scale and vice versa.
    /// </summary>
    public partial class Volume : Setting {
        // === Fields && Properties
        readonly List<VolumeModifier> _modifiers = new ();
        
        public AudioGroup Group { get; }
        public sealed override string SettingName { get; }
        SliderOption Option { get; }
        
        public override IEnumerable<PrefOption> Options => Option.Yield();
        public float ModifiedValue => BaseValue * ModifiersValue;
        float BaseValue => Option.Value;
        float ModifiersValue => _modifiers.Any() ? _modifiers.Min(m => m.Value) : 1f;

        // === Constructor
        public Volume(AudioGroup group, float defaultVolume = 1) {
            Group = group;
            SettingName = group.SettingName();
            Option = new SliderOption($"{Group}Volume", SettingName, 0f, 1f, false, NumberWithPercentFormat, defaultVolume, true, 0.1f);
            Option.onChange += SetVolume;
            SetVolume(Option.Value);
        }
        
        // === Operations
        public void AddModifier(VolumeModifier modifier) {
            _modifiers.Add(modifier);
        }
        
        public void RemoveModifier(VolumeModifier modifier) {
            _modifiers.Remove(modifier);
        }
        
        public void UpdateVolume() {
            AudioManager.SetAudioChannelVolume(Group, ModifiedValue);
        }

        void SetVolume(float volume) {
            AudioManager.SetAudioChannelVolume(Group, volume * ModifiersValue);
        }
    }

    public class VolumeModifier {
        public float Value { get; private set; }

        public VolumeModifier(float value) {
            Value = value;
        }

        public void SetTo(float value) {
            Value = value;
        }
    }
}
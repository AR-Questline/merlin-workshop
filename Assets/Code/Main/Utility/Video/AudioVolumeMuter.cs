using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Settings.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Maths.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Video {
    public partial class AudioVolumeMuter : Element<Model>, UnityUpdateProvider.IWithUpdateGeneric {
        public override ushort TypeForSerialization => SavedModels.AudioVolumeMuter;

        const float DefaultTweakTime = 2f;
        
        readonly float _tweakSpeed;
        VolumeModifier _volumeModifier;
        Volume _volume;
        DelayedValue _delayedValue;
        AudioGroup _group;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        AudioVolumeMuter() {}

        public AudioVolumeMuter(AudioGroup group, float targetMultiplier = 0f, float tweakTime = DefaultTweakTime) {
            _group = group;
            _tweakSpeed = (1 / tweakTime) * (1 - targetMultiplier);

            _volumeModifier = new VolumeModifier(1f);
            _delayedValue.SetInstant(1f);
            _delayedValue.Set(targetMultiplier);
        }

        protected override void OnInitialize() {
            _volume = World.All<Volume>().FirstOrDefault(v => v.Group == _group);
            if (_volume) {
                _volume.AddModifier(_volumeModifier);
            } else {
                World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelInitialized<Volume>(), this,
                    model => {
                        if (model is Volume v && v.Group == _group) {
                            _volume = v;
                            _volume.AddModifier(_volumeModifier);
                        }
                    }
                );
            }
            
            World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop) {
                DiscardModifier();
                return;
            }
            
            _delayedValue.Set(1f);
            if (!_delayedValue.IsStable) {
                World.Services.Get<UnityUpdateProvider>().RegisterGeneric(this);
            }
        }
        
        public void UnityUpdate() {
            _delayedValue.Update(Time.unscaledDeltaTime, _tweakSpeed);
            _volumeModifier.SetTo(_delayedValue.Value);
            _volume.UpdateVolume();
            
            if (_delayedValue.IsStable) {
                if (_delayedValue.Target == 1f) {
                    DiscardModifier();
                } else {
                    World.Services.Get<UnityUpdateProvider>().UnregisterGeneric(this);
                }
            }
        }

        void DiscardModifier() {
            _volume?.RemoveModifier(_volumeModifier);
            World.Services.Get<UnityUpdateProvider>().UnregisterGeneric(this);
        }
    }
}
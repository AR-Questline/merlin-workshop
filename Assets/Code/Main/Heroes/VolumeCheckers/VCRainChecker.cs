using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    [RequireComponent(typeof(Collider))]
    public sealed class VCRainChecker : VCHeroVolumeChecker, IRainIntensityModifier {
        const string RainIntensityParam = "RainIntensity";

        readonly Dictionary<GameObject, EnteredVolumeData> _emitters = new();

        WeatherController _weatherController;
        WeatherController WeatherController => _weatherController ?? World.Any<WeatherController>();
        AudioCore AudioCore => Services.Get<AudioCore>();
        public Component Owner => this;
        public float MultiplierWhenUnderRoof { get; private set; }

        protected override void OnAttach() {
            MultiplierWhenUnderRoof = Services.Get<CommonReferences>().AudioConfig.rainIntensityMultiplierWhenUnderRoof;
            base.OnAttach();
        }

        protected override void OnFirstVolumeEnter(Collider other) {
            AudioCore.SetRainIntensityMultiplier(this);
        }

        protected override void OnEnter(Collider triggerEntered) {
            if (triggerEntered == null) {
                return;
            }

            GameObject enteredGameObject = triggerEntered.gameObject;
            if (!_emitters.TryGetValue(enteredGameObject, out EnteredVolumeData data)) {
                StudioEventEmitter[] emitters = triggerEntered.GetComponentsInChildren<StudioEventEmitter>();
                _emitters[enteredGameObject] = data = new EnteredVolumeData(emitters);
            }
            data.AddTrigger(triggerEntered);

            UpdateRainIntensity();
        }

        protected override void OnStay() {
            UpdateRainIntensity();
        }

        protected override void OnExit(Collider triggerExited, bool destroyed = false) {
            if (triggerExited == null) {
                return;
            }

            GameObject exitedGameObject = triggerExited.gameObject;
            if (_emitters.TryGetValue(exitedGameObject, out EnteredVolumeData data)) {
                data.RemoveTrigger(triggerExited);
                if (data.IsEmpty) {
                    _emitters.Remove(exitedGameObject);
                }
            }
        }

        protected override void OnAllVolumesExit() {
            AudioCore.RestoreRainIntensityMultiplier(this);
        }

        void UpdateRainIntensity() {
            float rainIntensity = WeatherController?.RainIntensity ?? 0;
            foreach (var emitter in _emitters.Values.SelectMany(v => v.emitters)) {
                //emitter.SetParameter(RainIntensityParam, rainIntensity);
            }
        }

        protected override void OnDiscard() {
            _emitters.Clear();
            base.OnDiscard();
        }

        class EnteredVolumeData {
            public readonly StudioEventEmitter[] emitters;
            readonly List<Collider> _triggers;
            bool _isPlaying;

            public bool IsEmpty => _triggers.Count <= 0;

            public EnteredVolumeData(StudioEventEmitter[] emitters) {
                this.emitters = emitters;
                _triggers = new List<Collider>();
                _isPlaying = false;
            }

            public void AddTrigger(Collider collider) {
                _triggers.Add(collider);
                if (!_isPlaying) {
                    //emitters.ForEach(static e => e.Play());
                    _isPlaying = true;
                }
            }

            public void RemoveTrigger(Collider collider) {
                _triggers.Remove(collider);
                if (IsEmpty) {
                    //emitters.ForEach(static e => e.Stop());
                    _isPlaying = false;
                }
            }
        }
    }
}
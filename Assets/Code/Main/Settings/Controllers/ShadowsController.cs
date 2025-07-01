using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.MVC;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Settings.Controllers {
    [RequireComponent(typeof(Volume))]
    public class ShadowsController : StartDependentView<Shadows>, IVolumeController {
        Volume _volume;

        bool _hasShadows;
        float _originalShadowsDistance;
        HDShadowSettings _shadows;

        bool _hasContactShadows;
        ContactShadows _contactShadows;

        protected override void OnInitialize() {
            _volume = GetComponent<Volume>();
            Target.ListenTo(Setting.Events.SettingRefresh, OnSettingChanged, this);

            OnSettingChanged(Target);
        }

        public void NewVolumeProfileLoaded() {
            if (Target == null || _volume == null) {
                return;
            }
            _shadows = null;
            _contactShadows = null;
            OnSettingChanged(Target);
        }

        void OnSettingChanged(Setting setting) {
            if (_shadows == null) {
                ExtractShadows();
            }
            if (_contactShadows == null) {
                ExtractContactShadows();
            }

            var shadows = (Shadows)setting;
            if (_shadows) {
                _shadows.active = _hasShadows && shadows.ShadowsEnabled;
                if (_hasShadows) {
                    _shadows.maxShadowDistance.Override(shadows.ShadowsDistance * _originalShadowsDistance);
                }
            }

            if (_contactShadows) {
                _contactShadows.active = _hasContactShadows && shadows.ContactShadowsEnabled;
            }
        }

        void ExtractShadows() {
            if (!_volume.TryGetVolumeComponent(out _shadows)) {
                return;
            }
            _hasShadows = _shadows.active;
            _originalShadowsDistance = _shadows.maxShadowDistance.value;
        }
        void ExtractContactShadows() {
            if (!_volume.TryGetVolumeComponent(out _contactShadows)) {
                return;
            }
            _hasContactShadows = _contactShadows.active;
        }
    }
}

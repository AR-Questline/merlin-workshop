using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public partial class ActivateFastTravelElement : Element<Location>, IRefreshedByAttachment<ActivateFastTravelAttachment> {
        public override ushort TypeForSerialization => SavedModels.ActivateFastTravelElement;

        static readonly Vector3 FinalScale = Vector3.one;
        const float FinalIntensity = 524288f;
        const float FinalRange = 10;
        
        [Saved] bool _activated;
        ActivateFastTravelAttachment _spec;
        Tween _activeTween;
        bool _shouldBeEnabled;
        bool _wasEnabled = true;

        int _fadeID;
        
        public void InitFromAttachment(ActivateFastTravelAttachment spec, bool isRestored) {
            _spec = spec;
            if (_spec.go) {
                _spec.go.SetActive(false);
                _fadeID = Shader.PropertyToID("Fade");
            }
        }

        protected override void OnFullyInitialized() {
            if (_spec.go == null || _spec.lightRef == null) {
                string logInfo = LogUtils.GetDebugName(ParentModel) + ": " + (ParentModel?.ViewParent != null ? ParentModel.ViewParent.name : "");
                Log.Important?.Error($"Activate fast travel attachment {logInfo} has wrong or no parameters set.", _spec);
                return;
            }
            
            if (_activated) {
                _spec.go.SetActive(true);
                _spec.transform.localScale = FinalScale;
                _spec.lightRef.intensity = FinalIntensity;
                _spec.lightRef.range = FinalRange;
            }
            BandChanged(LocationCullingGroup.LastBand);
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, BandChanged, this);
        }

        void BandChanged(int newBand) {
            _shouldBeEnabled = LocationCullingGroup.InActiveLogicBands(newBand); 
            if (_activated) {
                EnsureCorrectState();
            }
        }

        void EnsureCorrectState() {
            if (_shouldBeEnabled) {
                if (!_wasEnabled) {
                    FadeIn();
                }
            } else {
                if (_wasEnabled) {
                    FadeOut();
                }
            }
        }

        public void ActivateTween() {
            _activated = true;

            _spec.go.SetActive(true);
            _spec.lightRef.intensity = 0.125f;
            _spec.lightRef.range = 0.001f;

            Sequence activateLight = DOTween.Sequence()
                                            .AppendInterval(0.23f)
                                            .Insert(0, _spec.lightRef.DOIntensity(4194304, 0.35f))
                                            .Insert(0.35f, _spec.lightRef.DOIntensity(FinalIntensity, 0.65f))
                                            .Insert(0, DOTween.To(() => _spec.lightRef.range, x => _spec.lightRef.range = x, FinalRange, 1f));

            activateLight.Play();
            EnsureCorrectState();
        }

        void FadeOut() {
            if (_activeTween != null) {
                _activeTween.Kill();
                _activeTween = null;
            }
            
            _wasEnabled = false;

            _activeTween = DOTween.To(() => _spec.vfxRef.GetFloat(_fadeID), 
                                      x => _spec.vfxRef.SetFloat(_fadeID, x), 
                                      0, 
                                      1f)
                .OnComplete(() => {
                    _spec.go.SetActive(false);
                    _activeTween = null;
                });
        }

        void FadeIn() {
            if (_activeTween != null) {
                _activeTween.Kill();
                _activeTween = null;
            }
            _wasEnabled = true;
            _spec.go.SetActive(true);

            _activeTween = DOTween.To(() => _spec.vfxRef.GetFloat(_fadeID),
                                      x => _spec.vfxRef.SetFloat(_fadeID, x),
                             1,
                              1f);
        }
    }
}
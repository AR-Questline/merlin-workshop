using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Audio {
    [UsesPrefab("Hero/VNonSpatialVoiceOvers")]
    [RequireComponent(typeof(ARFmodEventEmitter))]
    public class VNonSpatialVoiceOvers : View<NonSpatialVoiceOvers> {
        public ARFmodEventEmitter EventEmitter { get; private set; }

        VHeroController _vHeroController;
        Transform _myTransform;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().DefaultForHero();

        protected override void OnInitialize() {
            _myTransform = transform;
            EventEmitter = GetComponent<ARFmodEventEmitter>();
            Target.ParentModel.OnVisualLoaded(() => {
                _vHeroController = Target.ParentModel.VHeroController;
            });
        }

        void Update() {
            if (_vHeroController == null) {
                return;
            }
            var headTransform = _vHeroController.CinemachineHeadTarget?.transform;
            if (headTransform == null) {
                return;
            }
            var headForward = headTransform.forward;
            _myTransform.position = headTransform.position + headForward * 2f;
            _myTransform.forward = headForward * -1;
        }

        public void PlayVoiceOver(EventReference eventReference) {
            //EventEmitter.PlayNewEventWithPauseTracking(eventReference);
        }

        public void Stop() {
            //EventEmitter.Stop();
        }
    }
}

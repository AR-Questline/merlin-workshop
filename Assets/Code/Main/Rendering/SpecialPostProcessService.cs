using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Rendering {
    public class SpecialPostProcessService : MonoBehaviour, IService {
        [field: SerializeField] public VolumeWrapper VolumeWyrdskillSlomotion { get; private set; }
        [field: SerializeField] public VolumeWrapper VolumeStaminaUsedUp { get; private set; }
        [field: SerializeField] public VolumeWrapper VolumeWyrdnessTransition { get; private set; }
        [field: SerializeField] public VolumeWrapper VolumeSpyglass { get; private set; }
        [field: SerializeField] public VolumeWrapper VolumeDirectionalBlur { get; private set; }
        [field: SerializeField] public VolumeWrapper VolumeDrunk { get; private set; }
        [field: SerializeField] public VolumeWrapper VolumeHigh { get; private set; }

        void Update() {
            VolumeWyrdskillSlomotion.Update();
            VolumeStaminaUsedUp.Update();
            VolumeWyrdnessTransition.Update();
            VolumeSpyglass.Update();
            VolumeDirectionalBlur.Update();
            VolumeDrunk.Update();
            VolumeHigh.Update();
        }
    }
}
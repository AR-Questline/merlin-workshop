using FMODUnity;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations {
    public class OgreReelPusherFootSteps : MonoBehaviour {
        [SerializeField] public EventReference footStepEvent;
        [SerializeField] GameObject rightFeet, leftFeet;
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public void FootStep(int index) {
            //RuntimeManager.PlayOneShotAttached(footStepEvent, index == 0 ? rightFeet : leftFeet, this);
        }
    }
}

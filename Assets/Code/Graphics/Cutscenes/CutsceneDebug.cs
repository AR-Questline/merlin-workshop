using Awaken.TG.Graphics.Transitions;
using UnityEngine;

namespace Awaken.TG
{
    public class CutsceneDebug : MonoBehaviour {
        [SerializeField] TransitionBlinking _transitionBlinking;
        [SerializeField] Camera _camera;
        [SerializeField] TransitionBlinking.Data _blinkingData;

        void OnEnable() {
            StartBlinking();
        }

        void StartBlinking() {
            _transitionBlinking.Blink(_blinkingData, _camera);
        }
    }
}

using DG.Tweening;
using UnityEngine;

namespace Awaken.TG
{
    public class SimpleActivateOnTrigger : MonoBehaviour {
        [SerializeField] GameObject[] _gameObjectsToToggle = null;
        [SerializeField] GameObject[] _gameObjectsToDisable = null;
        [SerializeField] float _delay = 0f;
        [SerializeField] bool _onlyOnce;
        float _waitDuration = 1f;
        float _waitTimePassed = 0f;
        bool _wasTriggered;
        void OnTriggerEnter(Collider other) {
            if (_wasTriggered && _waitTimePassed < _waitDuration)
                return;
            _waitTimePassed = 0f;
            _wasTriggered = true;
            if (_delay > 0) {
                DOTween.Kill(this);
                DOVirtual.DelayedCall(_delay, ToggleObjects).SetId(this);
            } else {
                ToggleObjects();
            }
            if (_onlyOnce)
                gameObject.SetActive(false);
        }

        void Update() {
            if (_wasTriggered && _waitTimePassed <= _waitDuration)
                _waitTimePassed += Time.deltaTime;
        }

        void OnDestroy() {
            DOTween.Kill(this);
        }

        void ToggleObjects() {
            for (int i = 0; i < _gameObjectsToToggle.Length; i++) {
                _gameObjectsToToggle[i].SetActive(false);
                _gameObjectsToToggle[i].SetActive(true);
            }
            for (int i = 0; i < _gameObjectsToDisable.Length; i++) {
                _gameObjectsToDisable[i].SetActive(false);
            }
        }
    }
}

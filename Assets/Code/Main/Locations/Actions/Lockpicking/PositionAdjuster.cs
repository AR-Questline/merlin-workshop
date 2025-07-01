using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    public class PositionAdjuster : MonoBehaviour {
        [SerializeField] Vector3[] positions = Array.Empty<Vector3>();
        // This parameter is controlled by Animator
        [SerializeField, Range(0f, 1f)] float _t;
        [ShowInInspector] int _currentPosition;
        [ShowInInspector] int _nextPosition;

        void Awake() {
            SetPosition(_currentPosition);
        }

        void Update() {
            if (_t >= 1-Time.unscaledDeltaTime) {
                _currentPosition = _nextPosition;
            }
            transform.localPosition = Vector3.LerpUnclamped(positions[_currentPosition], positions[_nextPosition], _t);
        }

        public void SetPosition(int positionIndex) {
            _nextPosition = Mathf.Clamp(positionIndex, 0, positions.Length - 1);
            _t = 0;
        }
        
        //Editor only
        [Button]
        void SetPosition_(int pos) {
            pos = Mathf.Clamp(pos, 0, positions.Length - 1);
            _currentPosition = pos;
            transform.localPosition = positions[_currentPosition];
        }
        
        [ContextMenu(nameof(SetPosition))]
        void SetPosition() {
            SetPosition(_currentPosition);
        }
    }
}
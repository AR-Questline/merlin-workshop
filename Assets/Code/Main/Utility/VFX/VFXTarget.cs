using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Utility.VFX {
    [ExecuteInEditMode]
    public class VFXTarget : MonoBehaviour {
        [SerializeField] string _targetPositionPropertyName = "TargetPosition";
        [SerializeField] Transform[] _transformsToMove = null;
        [SerializeField] Vector3 _randomRangeMin = new(-0.1f, 0, -0.1f);
        [SerializeField] Vector3 _randomRangeMax = new(0.1f, 0, 0.1f);
        [SerializeField] bool _invertDirection;
        VisualEffect _visualEffect;
        Vector3 _lastTargetPosition;

        Vector3 TargetPosition => _visualEffect.GetVector3(_targetPositionPropertyName); 

        void Start() {
            _visualEffect = GetComponent<VisualEffect>();
        }

        void Update() {
            if (_lastTargetPosition != TargetPosition) {
                SetTarget();
            }
        }
        
        void SetTarget() {
            _lastTargetPosition = TargetPosition;
            
            var count = _transformsToMove.Length;
            var heading = _invertDirection ? (transform.position - _lastTargetPosition) : (_lastTargetPosition - transform.position);
            var distance = heading.magnitude;
            var direction = heading / distance;

            var chunkLength = distance / count;
            for (int i = 0; i < _transformsToMove.Length; i++)
            {
                var randomPos = new Vector3(
                    Random.Range(_randomRangeMin.x, _randomRangeMax.x),
                    Random.Range(_randomRangeMin.y, _randomRangeMax.y),
                    Random.Range(_randomRangeMin.z, _randomRangeMax.z));
                _transformsToMove[i].position = transform.position + (direction * chunkLength * i) + randomPos;
            }
        }
    }
}

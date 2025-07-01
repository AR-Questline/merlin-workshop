using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX
{
    [RequireComponent(typeof(VisualEffect))]
    public class VFXMeasuringDistance : MonoBehaviour{

        public string propertyName = "Arrow_Distance";

        private Ray _raycast;
        private RaycastHit _hit;
        private float _hitDistance;
        private bool _isHit;
        private LayerMask _layer = 1 << 11;
        private float _maxCheckDistance = 100;
        private VisualEffect _visualEffect;

        private void Awake(){
            _visualEffect = gameObject.GetComponent<VisualEffect>();
        }

        private void Update(){
            _raycast = new Ray(transform.position, transform.forward);
            _isHit = false;

            if (Physics.Raycast(_raycast, out _hit, _maxCheckDistance, _layer)){
                _isHit = true;
                _hitDistance = Vector3.Distance(transform.position, _hit.point);
                _visualEffect.SetFloat(propertyName, _hitDistance);
            }
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos(){
            if (_isHit){
                Debug.DrawRay(transform.position, transform.forward * Vector3.Distance(transform.position, _hit.point), Color.green);
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                var textOffset = new Vector3(1, 0, 0);
                UnityEditor.Handles.Label(_hit.point + textOffset, "Distance: " + _hitDistance, style);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_hit.point, 0.5f);
            }else{
                Debug.DrawRay(transform.position, transform.forward * _maxCheckDistance, Color.red);
            }
        }
        #endif
    }
}

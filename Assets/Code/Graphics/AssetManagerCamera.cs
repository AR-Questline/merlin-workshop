using System.Collections.Generic;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Graphics{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class AssetManagerCamera : MonoBehaviour{

        public List<Vector3> targets;
        public Vector3 offset = new Vector3(0,10,-40);
        public float smoothFactor = 0.5f;
        public float orbitSpeed = 5f;
        
        [UnityEngine.Scripting.Preserve] Camera _camera;
        GameObject _assetManager;
        bool _orbit = true;

        // =================================================
        void Start(){
            _camera = GetComponent<Camera>();
            offset = transform.position - targets[0];

            GetAllTargets();
        }

        void OnEnable(){
            GetAllTargets();
        }

        void OnDisable(){
            targets.Clear();
        }

        void LateUpdate() {
            if (targets.Count == 0){ return; }

            if (_orbit) {
                Quaternion camTurnAngle = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * orbitSpeed, Vector3.up);
                offset = camTurnAngle * offset;
            }
            Move();
        }

        void Move() {
            Vector3 newPosition = targets[0] + offset;
            transform.position = Vector3.Slerp(transform.position, newPosition, smoothFactor);
            transform.LookAt(targets[0], Vector3.up);
        }

        public void GetAllTargets(){
            _assetManager = GameObject.Find("AssetManager");
            if (_assetManager != null){
                targets.Clear();
                var childCount = _assetManager.transform.childCount;
                for (int i = 0; i < childCount; i++){
                    var child = _assetManager.transform.GetChild(i);
                    var childCollider = child.GetComponentInChildren<Collider>();
                    var center = childCollider.bounds.center;

                    if (child.GetComponentInChildren<Collider>() != null) {
                        targets.Add(center);
                    } else {
                        targets.Add(child.transform.position);
                    }
                }
            }
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.yellow;
            
            // Draw all camera targets
            if (targets != null) {
                for (int i = 0; i < targets.Count; i++){
                    GizmosUtil.DrawCross3D(targets[i], 4f);
                    Gizmos.DrawWireSphere(targets[i], 1f);
                }
            }
        }
    }
}

/* GetAllTargets update if AssetManager.childCount changed
 * 
 * 
 * 
 */
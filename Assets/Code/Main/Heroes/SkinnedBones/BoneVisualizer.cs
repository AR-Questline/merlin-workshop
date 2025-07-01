using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.SkinnedBones {
    public class BoneVisualizer : MonoBehaviour {
        [Range(0.001f, 0.3f)] public float sphereSize = 0.02f;

        void Awake() {
            Destroy(this);
        }
        
        void OnDrawGizmos() {
            Queue<Transform> transforms = new Queue<Transform>();
            foreach (Transform child in transform) {
                if (child.GetComponent<SkinnedMeshRenderer>() == null) {
                    transforms.Enqueue(child);
                }
            }

            while (transforms.Count > 0) {
                var current = transforms.Dequeue();
                Gizmos.DrawWireSphere(current.position, sphereSize);
                
                Gizmos.DrawLine(current.position, current.parent.position);

                foreach (Transform cChild in current) {
                    if (cChild.GetComponent<SkinnedMeshRenderer>() == null) {
                        transforms.Enqueue(cChild);
                    }
                }
            }
        }
    }
}
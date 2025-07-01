using UnityEngine;

namespace Awaken.TG.Graphics
{
    public class LookAt : MonoBehaviour {
        public Transform cameraRef;

        void Update() 
        {
            Vector3 lookPos = cameraRef.position - transform.position;
            Quaternion lookRot = Quaternion.LookRotation(lookPos, Vector3.up);
            float eulerY = lookRot.eulerAngles.y;
            Quaternion rotation = Quaternion.Euler (0, eulerY, 0);
            transform.rotation = rotation;
        }
    }
}

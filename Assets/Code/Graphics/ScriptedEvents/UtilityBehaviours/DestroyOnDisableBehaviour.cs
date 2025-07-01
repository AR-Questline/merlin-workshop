using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG
{
    public class DestroyOnDisableBehaviour : MonoBehaviour {
        [SerializeField] UnityEngine.Object objectToDestroy;
        
        void OnDisable() {
            Object.Destroy(objectToDestroy);
        }
    }
}

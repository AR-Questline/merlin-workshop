using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Graphics {
    [RequireComponent(typeof(Volume)), DefaultExecutionOrder(-200)]
    public class ProfileInstanceCreator : MonoBehaviour {
        [SerializeField] bool _createInstanceInEditor = true;
        [SerializeField] bool _createInstanceInRuntime = true;
        
        void Awake() {
            if (Application.isEditor) {
                if (!_createInstanceInEditor) {
                    Destroy(this);
                    return;
                }
            } else {
                if (!_createInstanceInRuntime) {
                    Destroy(this);
                    return;
                }
            }
            var unused = GetComponent<Volume>().profile;
        }
    }
}

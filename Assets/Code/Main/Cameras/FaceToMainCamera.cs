using Awaken.TG.Main.Cameras.CameraStack;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Cameras {
    public class FaceToMainCamera : ViewComponent<Model> {
        CameraHandle _handle;
        
        protected override void OnAttach() {
            _handle = World.Only<CameraStateStack>().MainHandle;
        }

        void Update() {
            if (_handle?.Camera != null) {
                transform.forward = _handle.Camera.transform.forward;
            }
        }
    }
}
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Cameras.Controllers {
    public class VCTPPCameraHeight : ViewComponent<GameCamera>, ICameraController {
        public float height = 20;
        public float sensitivity = 5;
        Transform _cameraTransform;


        public void Init() {
            _cameraTransform = ((VGameCamera) ParentView).transform;
        }

        public void Refresh(bool active) {
            if (!active) {
                return;
            }

            _cameraTransform.Translate(0f, CalculateDelta(), 0f, Space.World);
        }

        float CalculateDelta() {
            float current = _cameraTransform.position.y;
            float target = 0;
            Hero hero = Hero.Current;
            if (hero != null) {
                target = hero.MainView.transform.position.y + height;
            }
            float error = target - current;
            return Time.deltaTime * error * sensitivity;
        }

        public void OnChanged(bool active) {}
    }
}

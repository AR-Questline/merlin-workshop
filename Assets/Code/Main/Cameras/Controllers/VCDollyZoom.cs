using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Cameras.Controllers {
    public class VCDollyZoom : ViewComponent<GameCamera>, ICameraController {
        Camera _camera;
        Transform _focus;

        const float Duration = 1.4f;
        const float Percent = .5f;
        const Ease ZoomEase = Ease.InOutCubic;

        float _startTime;
        Vector3 _startPos;

        bool _isZooming;

        float _initHeightAtDist;

        float _FOV;

        public void Init() {
            _camera = Camera.main;
        }

        public void Refresh(bool active) {
            if (!active) return;

            if (_isZooming) {
                _camera.transform.position = Vector3.Lerp(_startPos, _focus.position,
                    DOVirtual.EasedValue(0, Percent, (Time.time - _startTime) / Duration, ZoomEase));
                _FOV = FOV(_initHeightAtDist, Distance());
            }

            //FoV updated every frame to avoid its changing after zooming. To back to normal FoV you need to disable VCDollyZoom
            _camera.fieldOfView = _FOV;
        }

        public void OnChanged(bool active) {
            _isZooming = active;
            if (active) {
                _focus = Hero.Current?.VHeroController?.transform;
                _startTime = Time.time;
                _startPos = _camera.transform.position;
                _isZooming = true;
                _initHeightAtDist = HeightAtDistance(Distance());
                _FOV = _camera.fieldOfView;
                DOVirtual.DelayedCall(Duration, () => _isZooming = false);
            }
        }

        float Distance() => Vector3.Distance(_camera.transform.position, _focus.position);

        float HeightAtDistance(float distance) =>
            2.0f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        float FOV(float height, float distance) => 2.0f * Mathf.Atan2(height * 0.5f, distance) * Mathf.Rad2Deg;
    }
}

using System.Collections;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using UnityEngine;

namespace Awaken.TG.Main.Fights.FPP {
    public class WeaponSway : ViewComponent<Hero> {
        const float BaseMultiplier = 0.25f;
        [Header("Sway Settings"), SerializeField] float rotationSmooth = 8;
        [SerializeField] float horizontalInputSmooth = 8, verticalInputSmooth = 8;
        [SerializeField] float horizontalMultiplier = 1f, verticalMultiplier = 0.5f;
        PlayerInput _playerInput;
        bool _attached;
        float _mouseX, _mouseY;
        Quaternion _additionalRotation, _currentRotation;
        bool _useLateUpdate;

        protected override void OnAttach() {
            _playerInput = World.Only<GameUI>().Element<PlayerInput>();
            _additionalRotation = Quaternion.identity;
            _attached = true;
            if (GetComponentInParent<Animator>().updateMode == AnimatorUpdateMode.Fixed) {
                StartCoroutine(nameof(WaitForFixedUpdate));
            } else {
                _useLateUpdate = true;
            }
        }

        IEnumerator WaitForFixedUpdate() {
            while (gameObject != null && Target is {HasBeenDiscarded: false}) {
                UpdateSway();
                yield return new WaitForFixedUpdate();
            }
        }

        void LateUpdate() {
            if (_useLateUpdate) {
                UpdateSway();
            }
        }

        void UpdateSway() {
            if (!_attached) {
                return;
            }
            float deltaTime = Time.deltaTime;
            
            // get mouse input
            float currentX = _playerInput.LookInput.x * BaseMultiplier * horizontalMultiplier;
            float currentY = _playerInput.LookInput.y * BaseMultiplier * verticalMultiplier;
            
            _mouseX = Mathf.Lerp(_mouseX, currentX, horizontalInputSmooth * deltaTime);
            _mouseY = Mathf.Lerp(_mouseY, currentY, verticalInputSmooth * deltaTime);

            // calculate target rotation
            Quaternion rotationX = Quaternion.AngleAxis(-_mouseY, Vector3.right);
            Quaternion rotationY = Quaternion.AngleAxis(-_mouseX, Vector3.up);
            _additionalRotation = rotationX * rotationY;

            _currentRotation = Quaternion.Slerp(_currentRotation, _additionalRotation, rotationSmooth * deltaTime);
            // rotate 
            transform.localRotation *= _currentRotation;
        }
    }
}

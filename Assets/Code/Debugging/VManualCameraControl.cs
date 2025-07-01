using System.Linq;
using Awaken.TG.Main.Cameras;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Debugging
{
    [UsesPrefab("Debug/VManualCameraControl")]
    public class VManualCameraControl : View<GameCamera> {
        // === Editor properties

        public float translationSpeed = 10f;
        public float rotationSpeed = 60f;
        public float precisionSlowdown = 0.07f;

        public Transform cameraStick;
        public Transform referenceTransform;
        public bool autoStraighten = true;

        // === Initialization

        public override Transform DetermineHost() {
            cameraStick = Services.Get<ViewHosting>().OnCamera();
            referenceTransform = cameraStick.GetComponentInChildren<Camera>().transform;
            return cameraStick;
        }

        // === Reacting to inputs
       
        static readonly KeyCode[] InterestingKeys = new[] {
            KeyCode.Keypad4, KeyCode.Keypad8, KeyCode.Keypad6, KeyCode.Keypad2, 
            KeyCode.Keypad3, KeyCode.Keypad9, KeyCode.KeypadPlus, KeyCode.KeypadMinus
        };

        void Update() {
            float factor = 1f;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                factor = precisionSlowdown;
            }
            float step = Time.deltaTime * translationSpeed * factor;
            float rotStep = Time.deltaTime * rotationSpeed * factor;

            #if DEBUG
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
                // shift down, control rotation
                if (Input.GetKey(KeyCode.Keypad4)) RotateInLocalSpace(Vector3.up, -rotStep);
                if (Input.GetKey(KeyCode.Keypad8)) RotateInLocalSpace(Vector3.right, -rotStep);
                if (Input.GetKey(KeyCode.Keypad6)) RotateInLocalSpace(Vector3.up, rotStep);
                if (Input.GetKey(KeyCode.Keypad2)) RotateInLocalSpace(Vector3.right, rotStep);
                if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Keypad9)) RotateInLocalSpace(Vector3.forward, rotStep);
                if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Keypad3)) RotateInLocalSpace(Vector3.forward, -rotStep);
            } else {
                // nothing down, translate
                if (Input.GetKey(KeyCode.Keypad4)) TranslateInLocalSpace(Vector3.left * step);
                if (Input.GetKey(KeyCode.Keypad8)) TranslateInLocalSpace(Vector3.up * step);
                if (Input.GetKey(KeyCode.Keypad6)) TranslateInLocalSpace(Vector3.right * step);
                if (Input.GetKey(KeyCode.Keypad2)) TranslateInLocalSpace(Vector3.down * step);
                if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Keypad9)) TranslateInLocalSpace(Vector3.forward * step);
                if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Keypad3)) TranslateInLocalSpace(Vector3.back * step);
            }

            if (Input.GetKey(KeyCode.Keypad0)) ResetTransform();
            
            // taking and releasing control from the usual suspects
            if (Input.GetKey(KeyCode.Escape)) ReleaseControl();
            bool somethingHappened = InterestingKeys.Any(kc => Input.GetKey(kc));
            if (somethingHappened) {
                AssumeControl();
                if (autoStraighten) StraightenHorizon();
            }
            #endif
        }

        // === UI facing methods

        public void PerformTranslate(Vector3 dir, float inputMagnitude) {
            AssumeControl();
            TranslateInLocalSpace(inputMagnitude * Time.deltaTime * translationSpeed * dir);
        }

        public void PerformRotate(Vector3 axis, float inputMagnitude) {
            AssumeControl();
            RotateInLocalSpace(axis, Time.deltaTime * rotationSpeed * inputMagnitude);
            if (autoStraighten) StraightenHorizon();
        }

        public void PerformReset() {
            AssumeControl();
            ResetTransform();
        }

        public void PerformStraighten() {
            AssumeControl();
            StraightenHorizon();
        }

        // === Helpers

        Transform Reference => referenceTransform != null ? referenceTransform : transform;

        void AssumeControl() => Target.TakeManualControl();
        void ReleaseControl() => Target.ReleaseManualControl();

        void ResetTransform() {
            cameraStick.localPosition = Vector3.zero;
            cameraStick.localRotation = Quaternion.identity;            
        }

        void TranslateInLocalSpace(Vector3 translation) {            
            Vector3 localT = Reference.TransformDirection(translation);
            cameraStick.localPosition += localT;
        }

        void RotateInLocalSpace(Vector3 localAxis, float amount) {
            Vector3 axis = Reference.TransformDirection(localAxis);
            cameraStick.RotateAround(Reference.position, axis, amount);
        }

        void StraightenHorizon() {
            Vector3 worldRight = Reference.transform.TransformDirection(Vector3.right);
            Vector3 newRight = worldRight;
            newRight.y = 0;
            cameraStick.rotation = Quaternion.FromToRotation(worldRight, newRight) * cameraStick.rotation;
        }
    }
}

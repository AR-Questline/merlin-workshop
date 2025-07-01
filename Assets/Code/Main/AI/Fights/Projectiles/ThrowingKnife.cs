using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class ThrowingKnife : Arrow {
        [SerializeField] bool rotateUsingHorizontalAxis = true;
        [SerializeField] float rotationSpeed = 720f;
        [SerializeField] Transform rotationPivot;
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference throwingKnifeTemplateRef;
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference brokenThrowingKnifeTemplateRef;
        [SerializeField] ARFmodEventEmitter eventEmitter;

        protected override ItemTemplate BrokenItemTemplate => brokenThrowingKnifeTemplateRef.Get<ItemTemplate>();
        ItemTemplate ThrowingKnifeTemplate => throwingKnifeTemplateRef.Get<ItemTemplate>();
        bool _shouldRotate = true;

        protected override void OnSetup(Transform firePoint) {
            base.OnSetup(firePoint);
            if (rotationPivot == null) {
                rotationPivot = transform;
            }
        }

        public void SetRotation(Quaternion newRotation) {
            if (rotationPivot == null || rotationPivot == transform) {
                return;
            }
            Quaternion rotation = rotateUsingHorizontalAxis ? Quaternion.Euler(newRotation.eulerAngles.x, 0, 0) : Quaternion.Euler(0, newRotation.eulerAngles.y, 0);
            rotationPivot.localRotation = rotation;
        }

        protected override void ProcessRotation(float deltaTime) {
            if (_shouldRotate) {
                if (rotateUsingHorizontalAxis) {
                    rotationPivot.Rotate(rotationPivot.right, rotationSpeed * deltaTime, Space.World);
                } else {
                    base.ProcessRotation(deltaTime);
                    rotationPivot.Rotate(Vector3.up, rotationSpeed * deltaTime, Space.Self);
                }
            }
        }

        protected override void OnContact(HitResult hitResult) {
            if (_itemTemplate == null) {
                SetItemTemplate(ThrowingKnifeTemplate);
            }
            base.OnContact(hitResult);
            
            if (!_destroyed) {
                return;
            }
            
            _shouldRotate = false;
            if (eventEmitter != null) {
                //eventEmitter.Stop();
                eventEmitter.enabled = false;
            }

            if (rotationPivot != transform) {
                rotationPivot.rotation = default;
            }
            transform.forward = -hitResult.Normal;
        }
    }
}
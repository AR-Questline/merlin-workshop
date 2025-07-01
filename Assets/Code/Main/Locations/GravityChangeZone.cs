using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations {
    public class GravityChangeZone : MonoBehaviour, ILogicReceiver {
        const float InnerActiveForce = 0.05f;
        const float InnerPassiveForce = 0.5f;
        const float InnerCenterPercent = 0.66f;
        const float GravityChangeSpeedForce = 20;
        [SerializeField] float upSpeed = 5;
        [SerializeField] float downSpeed = -5;
        [SerializeField] bool goingUp = true;
        [SerializeField] bool keepPlayerInCenter = true;
        [SerializeField] float modelRadiusMultiplier = 0.5f;
        float _innerCenterOffsetSquared;

        void Start() {
            float radius = (transform.parent != null)
                ? transform.parent.localScale.x * transform.localScale.x
                : transform.localScale.x; 
            _innerCenterOffsetSquared = radius * radius * modelRadiusMultiplier * modelRadiusMultiplier * InnerCenterPercent;
        }
        
        public void OnLogicReceiverStateSetup(bool state) => OnLogicReceiverStateChanged(state);

        public void OnLogicReceiverStateChanged(bool state) {
            SetGravityDirection(state);
        }

        [Button]
        public void ChangeGravityDirection() {
            SetGravityDirection(!goingUp);
        }

        void SetGravityDirection(bool goingUp) {
            this.goingUp = goingUp;
        }

        public float GetMaxGravityVelocity() {
            return goingUp ? upSpeed : downSpeed;
        }

        public float GetGravityForce() {
            return GetMaxGravityVelocity() > 0 ? GravityChangeSpeedForce : -GravityChangeSpeedForce;
        }

        public Vector3 GetDirectionTowardsCenter(Vector3 position, bool isPlayerMovingInside) {
            if (!keepPlayerInCenter) {
                return Vector3.zero;
            }
            Vector3 direction = (transform.position - position).ToHorizontal3();
            if (direction.sqrMagnitude <= _innerCenterOffsetSquared) {
                return Vector3.zero;
            }
            return direction.normalized * (isPlayerMovingInside ? InnerActiveForce : InnerPassiveForce);
        }
    }
}

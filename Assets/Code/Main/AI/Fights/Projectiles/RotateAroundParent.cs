using Awaken.TG.Assets;
using Awaken.TG.Main.Timing.ARTime;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class RotateAroundParent : TimeBehaviour {
        [SerializeField] float circleRadius = 1;
        [SerializeField] float lightHeightOffset = 0.33f;
        [SerializeField] float angleSpeed = 1f;
        [SerializeField] float lifeTime = 25f;
        [SerializeField, ARAssetReferenceSettings(new[] {typeof(GameObject)}, true)] 
        ShareableARAssetReference disappearVFX;
        
        Vector3 _heightOffset;
        Vector3 _positionOffset;
        float _angle;
        protected virtual Transform Center => transform.parent;
        
        protected override void Start() {
            base.Start();
            Init();
        }

        void OnDisable() {
            if (disappearVFX != null && disappearVFX.IsSet) {
                PrefabPool.InstantiateAndReturn(disappearVFX, transform.position, transform.rotation).Forget();
            }
        }

        protected virtual void Init() {
            Vector3 centerForward = Center.forward;
            float forwardDot = Vector3.Dot(centerForward, Vector3.forward);
            float rightDot = Vector3.Dot(centerForward, Vector3.right);
            _angle = Mathf.LerpUnclamped(Mathf.PI * -0.5f, 0, rightDot) * Mathf.Sign(forwardDot) * -1;
            
            transform.SetParent(Center, false);
            _positionOffset.Set(Mathf.Cos(_angle) * circleRadius, lightHeightOffset, Mathf.Sin(_angle) * circleRadius);
            transform.localPosition = _positionOffset;
            _heightOffset = new Vector3(0, lightHeightOffset, 0);
        }
        
        protected override void ProcessUpdate(float deltaTime) {
            if (gameObject.activeSelf == false || Center == null) {
                return;
            }

            _angle += deltaTime * angleSpeed;

            _positionOffset.x = Mathf.Cos(_angle) * circleRadius;
            _positionOffset.y = lightHeightOffset;
            _positionOffset.z = Mathf.Sin(_angle) * circleRadius;

            var centerPos = Center.position;
            transform.position = centerPos + _positionOffset;
            transform.LookAt(centerPos + _heightOffset);

            lifeTime -= deltaTime;
            if (lifeTime <= 0) {
                Destroy(gameObject);
            }
        }
    }
}
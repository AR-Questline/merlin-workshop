using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Regrowables.Moths {
    public class VFXMoths : MonoBehaviour {
        const string ScaredAwayVfxParameter = "ScaredAway";
        const float ScaredAwayDuration = 25f;
        [SerializeField] float scareAwayDistance = 10f;
        [SerializeField] new Collider collider;
        [SerializeField] VisualEffect vfx;
        bool _scaredAway;
        float _reappearTime;
        
        public bool CanUpdate => _reappearTime < Time.time;

        void OnEnable() {
            VfxMothUpdateHandler.RegisterVfxMoth(this);
        }

        void OnDisable() {
            VfxMothUpdateHandler.UnregisterVfxMoth(this);
        }
        
        public void UpdateMoths(Vector3 heroPos, bool heroCrouching) {
            var shouldEscape = !heroCrouching && Vector3.SqrMagnitude(transform.position - heroPos) < scareAwayDistance * scareAwayDistance;
            if (shouldEscape != _scaredAway) {
                _scaredAway = shouldEscape;
                vfx.SetBool(ScaredAwayVfxParameter, _scaredAway);
                if (_scaredAway) {
                    DisableInteraction();
                } else {
                    EnableInteraction();
                }
            }
        }

        void EnableInteraction() {
            collider.enabled = true;
        }

        void DisableInteraction() {
            collider.enabled = false;
            _reappearTime = Time.time + ScaredAwayDuration;
        }
    }
}

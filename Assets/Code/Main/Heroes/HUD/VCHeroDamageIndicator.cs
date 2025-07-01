using System.Collections;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroDamageIndicator : ViewComponent<Hero> {
        [SerializeField, Range(1.5f, 5f)] float damageIndicatorDuration = 1.5f;
        [SerializeField] CanvasGroup damageIndicator;
        [SerializeField] RectTransform directionalIndicator;
        
        Transform _attacker;
        
        protected override void OnAttach() {
            Target.AfterFullyInitialized(AfterFullyInitialized);
        }
        
        void AfterFullyInitialized() {
            Target.Element<HealthElement>().ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
        }
        
        void OnDamageTaken(DamageOutcome damageOutcome) {
            if (damageOutcome.Attacker != null) {
                _attacker = damageOutcome.Attacker.ParentTransform;
            } else {
                return;
            }
            
            StopCoroutine(nameof(RotateDamagePointerToTarget));
            StartCoroutine(nameof(RotateDamagePointerToTarget));
        }
        
        IEnumerator RotateDamagePointerToTarget() {
            float duration = damageIndicatorDuration;
            
            while (duration >= 0 && (_attacker != null)) {
                Vector3 direction = Target.MainView.transform.position - _attacker.position;
                Quaternion rotation = Quaternion.LookRotation(direction);
                rotation.z = -rotation.y;
                rotation.x = 0;
                rotation.y = 0;

                Vector3 northDirection = new(0, 0, Target.MainView.transform.eulerAngles.y);
                directionalIndicator.localRotation = rotation * Quaternion.Euler(northDirection);

                duration -= Time.deltaTime;
                damageIndicator.alpha = Mathf.Lerp(0, 1, Mathf.Log(duration + 1, damageIndicatorDuration));
                yield return null;
            }
            
            damageIndicator.alpha = 0;
        }
    }
}

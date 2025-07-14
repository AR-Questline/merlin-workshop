using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroDamageIndicator : ViewComponent<Hero> {
        [SerializeField, Range(1.5f, 5f)] float damageIndicatorDuration = 1.5f;
        [SerializeField] CanvasGroup damageIndicator;
        [SerializeField] RectTransform directionalIndicator;
        
        Transform _attacker;
        CancellationTokenSource _cts;
        
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
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            RotateDamagePointerToTargetAsync(_cts.Token).Forget();
        }
        
        async UniTaskVoid RotateDamagePointerToTargetAsync(CancellationToken token) {
            float duration = damageIndicatorDuration;
            
            while (duration >= 0 && _attacker != null && !token.IsCancellationRequested) {
                Vector3 direction = Target.MainView.transform.position - _attacker.position;
                Quaternion rotation = Quaternion.LookRotation(direction);
                rotation.z = -rotation.y;
                rotation.x = 0;
                rotation.y = 0;

                Vector3 northDirection = new(0, 0, Target.MainView.transform.eulerAngles.y);
                directionalIndicator.localRotation = rotation * Quaternion.Euler(northDirection);

                duration -= Time.unscaledDeltaTime;
                damageIndicator.alpha = Mathf.Lerp(0, 1, Mathf.Log(duration + 1, damageIndicatorDuration));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            
            damageIndicator.alpha = 0;
        }
        
        protected override void OnDestroy() {
            _cts?.Cancel();
            _cts = null;
        }
    }
}

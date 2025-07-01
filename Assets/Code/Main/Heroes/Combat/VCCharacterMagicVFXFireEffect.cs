using System.Threading;
using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;

namespace Awaken.TG.Main.Heroes.Combat {
    public class VCCharacterMagicVFXFireEffect : VCCharacterMagicVFX {
        float ChargeAmount => Target.ItemStats.ChargeAmount.ModifiedValue * 100;
        PositionConstraint _positionConstraint;
        CancellationTokenSource _cancellationToken;
        Quaternion _initialLocalRotation;
        
        protected override void Initialize() {
            _initialLocalRotation = transform.localRotation;
            base.Initialize();
            _positionConstraint = GetComponent<PositionConstraint>();
            transform.SetParent(null);
        }

        protected override void OnCastingSuccessfullyBegun() {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
            if (_visualEffect != null) {
                _visualEffect.gameObject.SetActive(false);
            }

            if (_positionConstraint != null) {
                _positionConstraint.constraintActive = true;
            }
        }

        protected override void OnCastingSuccessfullyEnded() {
            AsyncOnCastingSuccessfullyEnded().Forget();
        }

        protected async UniTaskVoid AsyncOnCastingSuccessfullyEnded() {
            transform.forward = Owner.MainView.transform.forward;
            transform.rotation *= _initialLocalRotation;
            if (_visualEffect != null) {
                _visualEffect.SetFloat("Charge", ChargeAmount);
                _visualEffect.gameObject.SetActive(true);
            }

            if (_positionConstraint != null) {
                _positionConstraint.constraintActive = false;
            }
            _cancellationToken = new CancellationTokenSource();
            bool success = await AsyncUtil.DelayTime(gameObject, disableVFXDelay, false, _cancellationToken);
            if (success) {
                if (_visualEffect != null) {
                    _visualEffect.gameObject.SetActive(false);
                }
                _positionConstraint.constraintActive = true;
            }
        }
    }
}
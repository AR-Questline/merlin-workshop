using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Executions;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.General {
    public class PendulumTrapController : MonoBehaviour {
        const float CloseToMaxTiltPercent = 0.95f;
        
        [SerializeField] EventReference movementAudio;
        [SerializeField] EventReference hitAudio;
        [SerializeField] float damage = 10f;
        [SerializeField] AnimationCurve angleCurve;
        [SerializeField] float time = 3f;
        [SerializeField] Transform bladePosition;
        
        Vector3 BladeWorldPosition => bladePosition.position;
        
        MitigatedExecution _mitigatedExecution;
        ListDictionary<ICharacter, int> _collisionCounter = new();
        Vector3 _previousBladePosition;
        float _currentTime;
        float _rotationX;
        float _rotationY;
        float _maxTilt;
        StudioEventEmitter _emitter;
        bool _movementAudioPaused;
        Transform _transform;

        void Start() {
            _transform = transform;
            _emitter = bladePosition.gameObject.GetComponent<StudioEventEmitter>();
            _rotationX = _transform.rotation.eulerAngles.x;
            _rotationY = _transform.rotation.eulerAngles.y;
            _mitigatedExecution = World.Services.Get<MitigatedExecution>();
            gameObject.SetUnityRepresentation(new IWithUnityRepresentation.Options {
                linkedLifetime = true,
            });
            _maxTilt = Mathf.Abs(angleCurve.Evaluate(0));
            //_emitter.PlayNewEventWithPauseTracking(movementAudio);
        }

        void FixedUpdate() {
            MovePendulum();
            
            _currentTime = (_currentTime + Time.fixedDeltaTime) % time;
        }

        void LateUpdate() {
            _previousBladePosition = BladeWorldPosition;
        }

        void OnTriggerEnter(Collider other) {
            if (other.TryGetComponentInParent(out NpcController npcController) && npcController.Target.TryGetElement(out NpcElement npcElement)) {
                CharacterTriggerEnter(npcElement);
            } 
            
            if (other.TryGetComponentInParent(out VHeroController heroController) && !other.TryGetComponent(out CharacterController characterController)) {
                CharacterTriggerEnter(Hero.Current);
            }
        }

        void CharacterTriggerEnter(ICharacter character) {
            if (!_collisionCounter.TryGetValue(character, out int counter)) {
                _mitigatedExecution.Register(DealDamageAction(character), character, MitigatedExecution.Cost.Heavy, MitigatedExecution.Priority.High, 0.1f);
                FMODManager.PlayOneShot(hitAudio, BladeWorldPosition);
                counter = 0;
            }

            _collisionCounter[character] = counter + 1;
        }

        void OnTriggerExit(Collider other) {
            if (other.TryGetComponentInParent(out NpcController npcController) && npcController.Target.TryGetElement(out NpcElement npcElement) && 
                _collisionCounter.TryGetValue(npcElement, out int npcColliderCounter)) {
                CharacterTriggerExit(npcElement, npcColliderCounter);
            } 
            
            if (other.TryGetComponentInParent(out VHeroController heroController) && _collisionCounter.TryGetValue(Hero.Current, out int heroColliderCounter)) {
                CharacterTriggerExit(Hero.Current, heroColliderCounter);
            }
        }

        void CharacterTriggerExit(ICharacter character, int counter) {
            if (counter == 1) {
                _collisionCounter.Remove(character);
            } else {
                _collisionCounter[character] = counter - 1;
            }
        }

        Action DealDamageAction(ICharacter receiver) {
            if (receiver != null && receiver.TryGetElement(out HealthElement healthElement)) {
                DamageParameters parameters = DamageParameters.Default;
                parameters.IsPrimary = false;
                parameters.Position = receiver.Coords;
                var direction = BladeWorldPosition - _previousBladePosition;
                parameters.Direction = direction;
                parameters.ForceDirection = direction.normalized;
                
                Damage damageToTake = new(parameters, receiver, receiver, new RawDamageData(damage));
                return () => healthElement.TakeDamage(damageToTake);
            }
            
            return null;
        }

        void MovePendulum() {
            var timePercent = (_currentTime / time) * 100f;
            var currentTilt = angleCurve.Evaluate(timePercent);
            _transform.localEulerAngles = new(_rotationX, _rotationY, currentTilt);
            var currentTiltPercent = Mathf.Abs(currentTilt / _maxTilt);
            
            if (currentTiltPercent >= CloseToMaxTiltPercent && !_movementAudioPaused) {
                //_emitter.Pause();
                _movementAudioPaused = true;
            } else if (_movementAudioPaused) {
                //_emitter.UnPause();
                _movementAudioPaused = false;
            }
        }
    }
}

using System;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using FMODUnity;
using UnityEngine;
using UnityEngine.Animations;

namespace Awaken.TG.Main.Fights.NPCs {
    [RequireComponent(typeof(ARFmodEventEmitter))]
    public class OutsideFoVAttackEmitter : MonoBehaviour {
        [SerializeField] ARFmodEventEmitter emitter;
        [SerializeField] ParentConstraint parentConstraint;
        
        OutsideFoVAttacksService _service;
        NpcElement _npc;
        IEventListener _damageDealtListener;
        bool _isPlaying;

        void Awake() {
            //emitter.EventStopTrigger = EmitterGameEvent.ObjectDisable;
        }

        public void Init(OutsideFoVAttacksService service) {
            _service = service;
        }
        
        public void AttachToNpcAndPlay(NpcElement npcElement) {
            _npc = npcElement;
            parentConstraint.AddSource(new ConstraintSource {
                sourceTransform = npcElement.Head,
                weight = 1
            });
            parentConstraint.constraintActive = true;
            //emitter.PlayNewEventWithPauseTracking(CommonReferences.Get.AudioConfig.AttackOutsideFOVWarningSound);
            _damageDealtListener = _npc.ListenTo(HealthElement.Events.BeforeDamageDealt, BeforeDamageDealt);
            _isPlaying = true;
        }

        void BeforeDamageDealt(Damage damage) {
            if (_isPlaying && damage.Target is Hero) {
                ReturnToPool();
            }
        }
        
        void Update() {
            if (!_isPlaying) {
                return;
            }
            
            // if (!emitter.IsPlaying()) {
            //     ReturnToPool();
            // }
        }

        void ReturnToPool() {
            _service.ReturnToPool(this);
            ClearReferences();
            //emitter.Stop();
        }

        void ClearReferences() {
            World.EventSystem.DisposeListener(ref _damageDealtListener);
            _npc = null;
            parentConstraint.RemoveSource(0);
            parentConstraint.constraintActive = false;
            _isPlaying = false;
        }
        
        void OnDestroy() {
            ClearReferences();
            _service = null;
        }
    }
}
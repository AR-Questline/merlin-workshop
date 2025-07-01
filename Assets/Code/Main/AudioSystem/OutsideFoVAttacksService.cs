using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public class OutsideFoVAttacksService : MonoBehaviour, IService {
        [SerializeField] OutsideFoVAttackEmitter emitterPrefab;
        readonly List<OutsideFoVAttackEmitter> _emittersPool = new();

        public void TryPlayAttackOutsideFOVWarning(NpcElement npc) {
            if (npc.GetCurrentTarget() is Hero && !AIUtils.IsInHeroViewCone(AIUtils.HeroDotToTarget(npc.Coords))) {
                OutsideFoVAttackEmitter emitter;
                if (_emittersPool.Count > 0) {
                    emitter = _emittersPool[^1];
                    _emittersPool.RemoveAt(_emittersPool.Count - 1);
                } else {
                    emitter = Object.Instantiate(emitterPrefab, transform);
                    emitter.Init(this);
                }
                emitter.AttachToNpcAndPlay(npc);
            }
        }
        
        public void ReturnToPool(OutsideFoVAttackEmitter emitter) {
            _emittersPool.Add(emitter);
        }
    }
}
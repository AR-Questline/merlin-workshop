using System;
using System.Threading;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class StopVFXOnDeathBehaviour : MonoBehaviour, IDeathBehaviour {
        [SerializeField] float delay = 0f;
        [SerializeField] VisualEffect[] vfxToStop = Array.Empty<VisualEffect>();

        CancellationTokenSource _cts;
        
        public bool UseDeathAnimation => false;
        public NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;

        public void OnVisualLoaded(DeathElement death, Transform transform) { }

        public void OnDeath(DamageOutcome damageOutcome, Location location) {
            if (delay > 0) {
                DelayStop().Forget();
            } else {
                Stop();
            }
        }
        
        async UniTaskVoid DelayStop() {
            if (delay > 0) {
                _cts = new CancellationTokenSource();
                if (!await AsyncUtil.DelayTime(this, delay, _cts.Token)) {
                    return;
                }
            }
            Stop();
        }

        void Stop() {
            foreach (var vfx in vfxToStop) {
                if (vfx != null) {
                    vfx.Stop();
                }
            }
        }

        void OnDestroy() {
            _cts?.Cancel();
            _cts = null;
        }
    }
}
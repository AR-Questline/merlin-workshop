using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.DamageInfo;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners.Critters {
    public class Critter : MonoBehaviour, IDamageable {
        [SerializeField, BoxGroup("Audio")] public StudioEventEmitter AudioEmitter;
        System.Action<Critter> onDeath; 
        public void Setup(System.Action<Critter> onDeath) {
            this.onDeath = onDeath;
        }
        
        public void OnAttacked() {
            onDeath?.Invoke(this);
            onDeath = null;
            Destroy(this);
        }
    }
}
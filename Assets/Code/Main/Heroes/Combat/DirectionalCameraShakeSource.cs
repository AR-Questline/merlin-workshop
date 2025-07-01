using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class DirectionalCameraShakeSource : MonoBehaviour {
        [SerializeField, Range(0f, 1f)] float force = 0.5f;
        [SerializeField] bool invokeOnEnable = true;
        [SerializeField] bool customImpulse;
        [SerializeField, ShowIf("customImpulse")] CinemachineImpulseSource impulseSource;

        public static class Events {
            public static readonly Event<Hero, DirectionalShakeData> InvokeShake = new(nameof(InvokeShake));
        }

        void OnEnable() {
            if (invokeOnEnable) {
                Hero.Current.Trigger(Events.InvokeShake, new DirectionalShakeData(force, transform.position, impulseSource));
            }
        }
    }

    public struct DirectionalShakeData {
        public float force;
        public Vector3 position;
        public CinemachineImpulseSource impulseSource;

        public DirectionalShakeData(float force, Vector3 position, CinemachineImpulseSource impulseSource) {
            this.force = force;
            this.position = position;
            this.impulseSource = impulseSource;
        }
    }
}

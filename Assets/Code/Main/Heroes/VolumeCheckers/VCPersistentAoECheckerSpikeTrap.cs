using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.VolumeCheckers {
    public class VCPersistentAoECheckerSpikeTrap : VCPersistentAoEChecker {
        [SerializeField] float spikeMovementSpeed = 0.5f;
        [SerializeField] float spikeMovementDistance = 0.4f;
        [SerializeField] GameObject staticSpikes;

        Vector3 _startingLocalPosition;
        
        protected override void OnAttach() {
            base.OnAttach();
            _startingLocalPosition = staticSpikes.gameObject.transform.localPosition;

            Target.AfterFullyInitialized(() => {
                if (Target.TryGetElement(out SpikeTrapPersistentAoE persistentAoEWithVFX)) {
                    var boxCollider = gameObject.GetComponent<BoxCollider>();
                    boxCollider.size = persistentAoEWithVFX.DamageCollider.size;
                    boxCollider.center = persistentAoEWithVFX.DamageCollider.center;
                    persistentAoEWithVFX.ListenTo(SpikeTrapPersistentAoE.Events.OnEffectWithVFXApplied, SpikesReturn, this);
                }
            }, this);
        }

        void Update() {
            staticSpikes.gameObject.transform.localPosition = Vector3.MoveTowards(staticSpikes.gameObject.transform.localPosition, _startingLocalPosition, spikeMovementSpeed * Time.deltaTime);
        }

        void SpikesReturn() {
            staticSpikes.gameObject.SetActive(false);
            var localPosition = _startingLocalPosition;
            localPosition = new Vector3(localPosition.x, localPosition.y, localPosition.z - spikeMovementDistance);
            staticSpikes.gameObject.transform.localPosition = localPosition;
            staticSpikes.gameObject.SetActive(true);
        }
    }
}
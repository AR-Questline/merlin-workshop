using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Awaken.Utility.Graphics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Main.Rendering {
    [Serializable]
    public class VolumeWrapper {
        [SerializeField] Volume volume;

        float _speed;
        float _desiredWeight = 0;

        List<(int owner, float weight, float speed)> _ownerWeightRequests = new();

        public void Update() {
            volume.weight = Mathf.MoveTowards(volume.weight, _desiredWeight, _speed * Time.unscaledDeltaTime);
        }
        
        public bool TryGetVolumeComponent<T>(out T component) where T : VolumeComponent {
            return volume.TryGetVolumeComponent(out component);
        }

        /// <summary>
        /// Use for overrides or in cases where only one owner is expected
        /// </summary>
        public void SetWeight(float weight, float speed) {
            if (_ownerWeightRequests.Count > 0) {
                _ownerWeightRequests.Clear();
            }
            _desiredWeight = weight;
            _speed = speed;
        }
        
        /// <summary>
        /// Use in cases where more than one owner can request a weight change
        /// </summary>
        public void SetOwnerWeight(int ownerID, float newWeight, float speed) {
            int reqIndex = _ownerWeightRequests.IndexOf(r => r.owner == ownerID);
            var previousRequest = reqIndex >= 0 ? _ownerWeightRequests[reqIndex] : default;
            
            if (reqIndex >= 0 && newWeight == 0) {
                // Setting to 0 means the owner does not have a requested weight anymore so we can remove it
                _ownerWeightRequests.RemoveAt(reqIndex);
                SetDesiredValues();
                return;
            }

            // New weight is 0 and owner did not have a weight before, nothing to do
            if (newWeight == 0) {
                return;
            }

            if (reqIndex >= 0) {
                _ownerWeightRequests[reqIndex] = (ownerID, newWeight, speed);
            } else {
                _ownerWeightRequests.Add((ownerID, newWeight, speed));
            }
            
            // If the new weight is higher than the desired weight, we can directly set the new weight as desired
            if (newWeight >= _desiredWeight) {
                _desiredWeight = newWeight;
                _speed = speed;
                return;
            }
            
            // If the previous weight was higher than the new weight and the old weight is close to the desired weight, we need to find a new weight
            if (previousRequest.weight > newWeight && Math.Abs(_desiredWeight - previousRequest.weight) < 0.005f) {
                SetDesiredValues();
            }
            
            // Desired weight is higher than the new weight, nothing more to do
        }

        void SetDesiredValues() {
            if (_ownerWeightRequests.Count == 0) {
                _desiredWeight = 0;
                return;
            }
            var foundDesired = _ownerWeightRequests.MaxBy(r => r.weight);
            _desiredWeight = foundDesired.weight;
            _speed = foundDesired.speed;
        }
    }
}
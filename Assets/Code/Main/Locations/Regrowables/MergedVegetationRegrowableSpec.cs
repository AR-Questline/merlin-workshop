using System;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Utility.Availability;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths.Data;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Regrowables {
    [SelectionBase]
    public class MergedVegetationRegrowableSpec : MonoBehaviour, IRegrowableSpec, IInteractableWithHeroProviderComplex {
        [SerializeField] ItemSpawningData[] itemReferences = Array.Empty<ItemSpawningData>();
        [SerializeField, TemplateType(typeof(CrimeOwnerTemplate))] TemplateReference[] crimeOwners = Array.Empty<TemplateReference>();
        [SerializeField] DayNightAvailability[] availabilities = Array.Empty<DayNightAvailability>();
        [SerializeField] string[] regrowablePartKeys = Array.Empty<string>();
        [SerializeField] ARTimeSpan[] regrowRates = Array.Empty<ARTimeSpan>();

        [SerializeField] AvailabilityRegrowableIndices[] availabilityToRegrowableIndices = Array.Empty<AvailabilityRegrowableIndices>();
        [SerializeField] SinglesData[] regrowablesData = Array.Empty<SinglesData>();

        Regrowable[] _regrowables;
        Dictionary<Collider, uint> _colliderToIndex = new Dictionary<Collider, uint>();
        Dictionary<uint, Collider> _indexToColliders = new Dictionary<uint, Collider>();

        public uint Count => (uint)regrowablesData.Length;
        public StoryBookmark StoryOnPickedUp => null;
        
        void Awake() {
            _regrowables = new Regrowable[regrowablesData.Length];

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                for (var i = 0u; i < availabilities.Length; i++) {
                    var availabilityIndex = i;
                    var availability = availabilities[i];
                    availability.Init(() => Spawn(availabilityIndex), () => Despawn(availabilityIndex));
                }
            }
        }

        void OnEnable() {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                foreach (var availability in availabilities) {
                    availability.Enable();
                }
            }
        }

        void OnDisable() {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                foreach (var availability in availabilities) {
                    availability.Disable();
                }
            }
        }

        void OnDestroy() {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                foreach (var availability in availabilities) {
                    availability.Deinit();
                }
            }

            _colliderToIndex = null;
            _indexToColliders = null;
        }

        public SpecId MVCId(uint localId) {
            return regrowablesData[localId].specId;
        }

        public SmallTransform Transform(uint localId) {
            return regrowablesData[localId].transform;
        }

        public string RegrowablePartKey(uint localId) {
            var keyIndex = regrowablesData[localId].regrowablePartKeyIndex;
            return regrowablePartKeys[keyIndex];
        }

        public ItemSpawningData ItemReference(uint localId) {
            var itemIndex = regrowablesData[localId].itemIndex;
            return itemReferences[itemIndex];
        }

        public ARTimeSpan RegrowRate(uint localId) {
            var regrowRateIndex = regrowablesData[localId].regrowRateIndex;
            return regrowRates[regrowRateIndex];
        }

        public CrimeOwnerTemplate CrimeOwner(uint localId) {
            var crimeOwnerIndex = regrowablesData[localId].crimeOwnerIndex;
            var owner = crimeOwners[crimeOwnerIndex];
            return owner is { IsSet: true } ? owner.Get<CrimeOwnerTemplate>() : null;
        }

        public void RegrowablePartSpawned(uint localId, GameObject spawnedRegrowablePart) {
            var regrowableComponent = spawnedRegrowablePart.GetComponentInChildren<Collider>(true);
            _colliderToIndex[regrowableComponent] = localId;
            _indexToColliders[localId] = regrowableComponent;
        }

        public void RegrowablePartDespawned(uint localId) {
            if (_indexToColliders.TryGetValue(localId, out var regrowableCollider)) {
                _colliderToIndex.Remove(regrowableCollider);
                _indexToColliders.Remove(localId);
            } else {
                Log.Important?.Error($"For merged regrowable: {this}[{this.gameObject.scene.name}], regrowable: {localId}, despawned collider not found", this);
            }
        }

        public IInteractableWithHero InteractableWithHero(Collider collider) {
            if (_colliderToIndex.TryGetValue(collider, out uint index)) {
                return _regrowables[index];
            }
            return null;
        }

        // === Main regrowable logic
        void Spawn(uint availabilityIndex) {
            var regrowableIndices = availabilityToRegrowableIndices[availabilityIndex];
            foreach (var regrowableIndex in regrowableIndices.indices) {
                var regrowable = new Regrowable(regrowableIndex, this);
                _regrowables[regrowableIndex] = regrowable;
                CullingSystemRegistrator.Register(regrowable);
                RegrowableInitialization.Initialize(regrowable);
            }
        }

        void Despawn(uint availabilityIndex) {
            var regrowableIndices = availabilityToRegrowableIndices[availabilityIndex];
            foreach (var regrowableIndex in regrowableIndices.indices) {
                var regrowable = _regrowables[regrowableIndex];
                CullingSystemRegistrator.Unregister(regrowable);
                RegrowableInitialization.Uninitialize(regrowable);
                _regrowables[regrowableIndex] = null;
            }
        }

        public static MergedVegetationRegrowableSpec Create(List<VegetationRegrowableSpec> singleSpecs, GameObject target) {
            Asserts.IsLessOrEqual(singleSpecs.Count, ushort.MaxValue);

            var mergedSpec = target.AddComponent<MergedVegetationRegrowableSpec>();

            var itemReferenceToIndex = new Dictionary<ItemSpawningData, ushort>();
            var crimeOwnerToIndex = new Dictionary<TemplateReference, ushort>();
            var availabilityToIndex = new Dictionary<DayNightAvailability, ushort>();
            var availabilityToRegrowablesData = new List<List<ushort>>();
            var regrowablePartKeyToIndex = new Dictionary<string, ushort>();
            var regrowRateToIndex = new Dictionary<ARTimeSpan, ushort>();

            var entries = new SinglesData[singleSpecs.Count];

            for (ushort i = 0; i < singleSpecs.Count; i++) {
                var regrowableSpec = singleSpecs[i];

                var itemReference = regrowableSpec.ItemReference;
                var itemReferenceIndex = GetIndex(itemReferenceToIndex, itemReference);

                var crimeOwner = regrowableSpec.CrimeOwnerTemplate;
                var crimeOwnerIndex = GetIndex(crimeOwnerToIndex, crimeOwner);

                var availability = regrowableSpec.Availability;
                var availabilityIndex = GetIndex(availabilityToIndex, availability);
                while (availabilityToRegrowablesData.Count <= availabilityIndex) {
                    availabilityToRegrowablesData.Add(new List<ushort>());
                }
                availabilityToRegrowablesData[availabilityIndex].Add(i);

                var regrowablePartKey = regrowableSpec.RegrowablePartKey(0);
                var regrowablePartKeyIndex = GetIndex(regrowablePartKeyToIndex, regrowablePartKey);

                var regrowRate = regrowableSpec.RegrowRate;
                var regrowRateIndex = GetIndex(regrowRateToIndex, regrowRate);

                entries[i] = new SinglesData {
                    specId = regrowableSpec.SceneId,
                    transform = regrowableSpec.Transform(0),

                    itemIndex = itemReferenceIndex,
                    crimeOwnerIndex = crimeOwnerIndex,
                    regrowablePartKeyIndex = regrowablePartKeyIndex,
                    regrowRateIndex = regrowRateIndex
                };
            }

            mergedSpec.itemReferences = ToArray(itemReferenceToIndex);
            mergedSpec.crimeOwners = ToArray(crimeOwnerToIndex);
            mergedSpec.availabilities = ToArray(availabilityToIndex);
            mergedSpec.regrowablePartKeys = ToArray(regrowablePartKeyToIndex);
            mergedSpec.regrowRates = ToArray(regrowRateToIndex);
            mergedSpec.availabilityToRegrowableIndices = new AvailabilityRegrowableIndices[availabilityToRegrowablesData.Count];
            for (int i = 0; i < availabilityToRegrowablesData.Count; i++) {
                var indices = availabilityToRegrowablesData[i];
                mergedSpec.availabilityToRegrowableIndices[i] = new AvailabilityRegrowableIndices(indices.ToArray());
            }
            mergedSpec.regrowablesData = entries;

            return mergedSpec;

            ushort GetIndex<T>(Dictionary<T, ushort> toIndex, T key) {
                if (toIndex.TryGetValue(key, out var index)) {
                    return index;
                }

                index = (ushort)toIndex.Count;
                toIndex.Add(key, index);
                return index;
            }

            T[] ToArray<T>(Dictionary<T, ushort> toIndex) {
                var array = new T[toIndex.Count];
                foreach (var kvp in toIndex) {
                    array[kvp.Value] = kvp.Key;
                }
                return array;
            }
        }

        [Serializable]
        struct SinglesData {
            public SpecId specId;
            public SmallTransform transform;

            public ushort itemIndex;
            public ushort crimeOwnerIndex;
            public ushort regrowablePartKeyIndex;
            public ushort regrowRateIndex;
        }

        [Serializable]
        struct AvailabilityRegrowableIndices {
            public ushort[] indices;

            public AvailabilityRegrowableIndices(ushort[] indices) {
                this.indices = indices;
            }
        }
    }
}

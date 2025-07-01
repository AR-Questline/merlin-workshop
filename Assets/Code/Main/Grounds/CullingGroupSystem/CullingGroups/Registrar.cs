using System;
using System.Runtime.CompilerServices;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups {
    class Registrar {
        const float GrowFactor = 1.2f;

        readonly CullingGroup _generatedGroup;

        BoundingSphere[] _spheres;
        Registree[] _registry;
        Registree[] _registreeCallbackRequests;
        UnsafeBitmask _pausedElements;
        bool isAnyAnyPaused;
        int _lastElementIndex;
        int _lastCallbackRequestIndex;
        bool _updatesBeingRunLock;
        public bool IsAnyPaused => isAnyAnyPaused;
        public bool NoElements => _lastElementIndex == -1;

        public Registrar(CullingGroup generatedGroup, int initialSize) {
            _spheres = new BoundingSphere[initialSize];
            _registry = new Registree[initialSize];
            _pausedElements = new UnsafeBitmask((uint)initialSize, ARAlloc.Persistent);
            _registreeCallbackRequests = new Registree[initialSize];
            
            _generatedGroup = generatedGroup;
            _lastElementIndex = -1;
            _lastCallbackRequestIndex = -1;
            generatedGroup.SetBoundingSpheres(_spheres);
        }
        
        public void AddRegistrySphereRef(Registree registree) {
            _lastElementIndex++;
            _pausedElements.EnsureCapacity((uint)_lastElementIndex + 1);
            _pausedElements[(uint)_lastElementIndex] = false;
            EnsureArraySize();
            
            Register(registree);
        }

        public bool TryRemoveRegistrySphereRef(Registree lodGroup) {
            int indexToSubstitute = lodGroup.RegistrarIndex;
            if (indexToSubstitute == -1) return false;

            if (indexToSubstitute == _lastElementIndex) {
                _generatedGroup.SetBoundingSphereCount(_lastElementIndex);
                _registry[indexToSubstitute] = null;
                _spheres[indexToSubstitute].position = Vector3.zero;
                _pausedElements[(uint)indexToSubstitute] = false;
                _lastElementIndex--;
                return true;
            }
            
            _generatedGroup.EraseSwapBack(indexToSubstitute);
            _generatedGroup.SetBoundingSphereCount(_lastElementIndex);
            EraseSwapBackRegistry(indexToSubstitute);
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registree GetRegistreeFromIndex(int sphereToFind) {
            if (sphereToFind < 0 || sphereToFind > _lastElementIndex) return null;

            return _registry[sphereToFind];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePosition(Registree registree, Vector3 newPosition) {
            _spheres[registree.RegistrarIndex].position = newPosition;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PauseElement(ICullingSystemRegistree element) {
            if (TryFindRegistreeIndex(element, out var index) && _pausedElements[(uint)index] == false) {
                _pausedElements[(uint)index] = true;
                isAnyAnyPaused = true;
                Registree registreeFromIndex = _registry[index];
                registreeFromIndex?.TriggerPausedEvent(true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpauseElement(ICullingSystemRegistree element) {
            if (TryFindRegistreeIndex(element, out var index) && _pausedElements[(uint)index]) {
                _pausedElements[(uint)index] = false;
                isAnyAnyPaused = _pausedElements.AnySet();
                Registree registreeFromIndex = _registry[index];
                registreeFromIndex?.TriggerPausedEvent(false);
            }
        }
        
        public bool TryGetElementPausedStatus(ICullingSystemRegistree element, out bool isPaused) {
            if (TryFindRegistreeIndex(element, out var index)) {
                isPaused = _pausedElements[(uint)index];
                return true;
            }
            isPaused = false;
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PauseAllUnpausedElements() {
            var prevUnpausedElements = _pausedElements.DeepClone(ARAlloc.Temp);
            prevUnpausedElements.Invert();
            TriggerPauseEventsOnPreviouslyUnpausedElements(prevUnpausedElements);
            _pausedElements.All();
            isAnyAnyPaused = true;
            prevUnpausedElements.Dispose();
        }
        
        void TriggerPauseEventsOnPreviouslyUnpausedElements(UnsafeBitmask previouslyUnpausedElements) {
            foreach (var previouslyUnpausedElementIndex in previouslyUnpausedElements.EnumerateOnes()) {
                // UnsafeBitmask cannot decrease size so need to check to not overshoot
                if (previouslyUnpausedElementIndex > _lastElementIndex) {
                    return;
                }
                try {
                    Registree registreeFromIndex = _registry[previouslyUnpausedElementIndex];
                    registreeFromIndex?.TriggerPausedEvent(true);
                } catch (Exception e) {
                    Debug.LogException(e);
                    Debug.Log("Exception on index " + previouslyUnpausedElementIndex);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnpauseAllPausedElements() {
            if (_lastElementIndex < 0) {
                return;
            }

            var prevPausedElements = _pausedElements.DeepClone(ARAlloc.Temp);
            _pausedElements.Clear();
            isAnyAnyPaused = false;
            TriggerUnpauseEventsOnPreviouslyPausedElements(prevPausedElements);
            prevPausedElements.Dispose();
        }

        void TriggerUnpauseEventsOnPreviouslyPausedElements(UnsafeBitmask previouslyPausedElements) {
            foreach (var previouslyPausedElementIndex in previouslyPausedElements.EnumerateOnes()) {
                // UnsafeBitmask cannot decrease size so need to check to not overshoot
                if (previouslyPausedElementIndex > _lastElementIndex) {
                    return;
                }
                try {
                    Registree registreeFromIndex = _registry[previouslyPausedElementIndex];
                    registreeFromIndex?.TriggerPausedEvent(false);
                } catch (Exception e) {
                    Debug.LogException(e);
                    Debug.Log("Exception on index " + previouslyPausedElementIndex);
                }
            }
        }
        
        public void RunScheduledUpdates() {
            _updatesBeingRunLock = true;
            if (_lastCallbackRequestIndex == -1) {
                _updatesBeingRunLock = false;
                return;
            }
            for (int i = _lastCallbackRequestIndex; i >= 0; i--) {
                _registreeCallbackRequests[i].UpdateDistanceBand();
                _registreeCallbackRequests[i] = null;
            }
            _lastCallbackRequestIndex = -1;
            _updatesBeingRunLock = false;
        }
        
        /// <summary>
        /// Should only be called as a result of the Unity CullingGroup callback
        /// </summary>
        public void ScheduleUpdate(Registree registree, int currentDistanceBand) {
            if (_updatesBeingRunLock) {
                Log.Critical?.Error("Trying to schedule update while updates are being run");
                return;
            }
            
            if ((!isAnyAnyPaused || _pausedElements[(uint)registree.RegistrarIndex] == false) && 
                registree.ScheduleUpdate(currentDistanceBand)) {
                _registreeCallbackRequests[++_lastCallbackRequestIndex] = registree;
            }
        }
        
        public void ScheduleUpdateAll(bool withHysteresis) {
            if (_updatesBeingRunLock) {
                Log.Critical?.Error("Trying to schedule update while updates are being run");
                return;
            }

            if (withHysteresis) {
                for (int i = 0; i <= _lastElementIndex; i++) {
                    ScheduleUpdateRegistreeWithHysteresis(i);
                }
            } else {
                for (int i = 0; i <= _lastElementIndex; i++) {
                    ScheduleUpdateRegistree(i);
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ScheduleUpdateRegistreeWithHysteresis(int i) {
            int dist = _generatedGroup.GetDistance(i);
            Registree registreeFromIndex = _registry[i];
            dist = BaseCullingGroup.HysteresisToBand(dist, registreeFromIndex.CurrentDistanceBand);
            ScheduleUpdate(registreeFromIndex, dist);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ScheduleUpdateRegistree(int i) {
            int dist = _generatedGroup.GetDistance(i);
            Registree registreeFromIndex = _registry[i];
            ScheduleUpdate(registreeFromIndex, dist);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureArraySize() {
            if (_lastElementIndex < _spheres.Length) return;

            var newSize = Mathf.RoundToInt(_lastElementIndex * GrowFactor);
            Array.Resize(ref _spheres, newSize);
            Array.Resize(ref _registry, newSize);
            Array.Resize(ref _registreeCallbackRequests, newSize);
            _pausedElements.EnsureCapacity((uint)newSize);
            _generatedGroup.SetBoundingSpheres(_spheres);
        }

        void Register(Registree registree) {
            registree.SetRegistrarIndex(_lastElementIndex);
            _registry[_lastElementIndex] = registree;
            _spheres[_lastElementIndex] = new BoundingSphere(registree.Coords, registree.Radius);
            _generatedGroup.SetBoundingSphereCount(_lastElementIndex + 1);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryFindRegistreeIndex(ICullingSystemRegistree registree, out int index) {
            // More likely that the searched element would be somewhere at the end
            for (int i = _lastElementIndex; i >= 0; i--) {
                if (_registry[i].Parent == registree) {
                    index = i;
                    return true;
                }   
            }
            
            index = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EraseSwapBackRegistry(int indexToSubstitute) {

            
            _registry[indexToSubstitute].SetRegistrarIndex(-1);
            _registry[indexToSubstitute] = _registry[_lastElementIndex]; // Move last item to erased index
            _registry[indexToSubstitute].SetRegistrarIndex(indexToSubstitute);
            
            // Clearing old data
            _registry[_lastElementIndex] = null; 
            _spheres[_lastElementIndex].position = Vector3.zero;
            
            var lastElementPausedState = _pausedElements[(uint)_lastElementIndex];
            _pausedElements[(uint)indexToSubstitute] = lastElementPausedState;
            _pausedElements[(uint)_lastElementIndex] = false;
            
            _lastElementIndex--;
        }

        public void Dispose() {
            _pausedElements.Dispose();
        }
    }
}
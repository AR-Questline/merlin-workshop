using System;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.Assertions;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public struct RaycastBatchCheck : IDisposable {
        const int MinCommandsPerJob = 4;
        NativeArray<OverlapSphereCommand> _overlapSphereCommands;
        NativeArray<RaycastCommand> _raycastCommands;
        NativeArray<ColliderHit> _overlapSphereHits;
        NativeArray<RaycastHit> _raycastHits;

        public void Init(int maxCount) {
            _overlapSphereCommands = new NativeArray<OverlapSphereCommand>(maxCount, ARAlloc.Persistent);
            _raycastCommands = new NativeArray<RaycastCommand>(maxCount, ARAlloc.Persistent);
            _overlapSphereHits = new NativeArray<ColliderHit>(maxCount, ARAlloc.Persistent);
            _raycastHits = new NativeArray<RaycastHit>(maxCount, ARAlloc.Persistent);
        }
        
        public void Set(int index, int layerMask, Vector3 origin, Vector3 direction, float maxDistance, float sphereRadius = 0.02f, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            if (index < 0 || index >= _raycastCommands.Length) {
                Log.Important?.Error($"Index {index} is out of range [0, {_raycastCommands.Length - 1}]");
                return;
            }
            
            _overlapSphereCommands[index] = new OverlapSphereCommand(origin, sphereRadius, new QueryParameters(layerMask, false, queryTriggerInteraction));
            
            _raycastCommands[index] =  new RaycastCommand(
                origin, direction, new QueryParameters(layerMask, false, queryTriggerInteraction), maxDistance);
        }
        
        public void ExecuteRaycasts() {
            var overlapSphereHandle = OverlapSphereCommand.ScheduleBatch(
                _overlapSphereCommands, 
                _overlapSphereHits, MinCommandsPerJob, 1);
            
            var raycastJobHandle = RaycastCommand.ScheduleBatch(
                _raycastCommands, 
                _raycastHits, MinCommandsPerJob);
            
            var overlapAndRaycastJob = JobHandle.CombineDependencies(overlapSphereHandle, raycastJobHandle);
            overlapAndRaycastJob.Complete();
            
            int overlapSphereHitsCount = 0;
            for (int i = 0; i < _overlapSphereHits.Length; i++) {
                if (_overlapSphereHits[i].instanceID != 0) {
                    var raycastCommand = _raycastCommands[i];
                    var overlapSphereCommand = _overlapSphereCommands[i];
                    raycastCommand.direction = _overlapSphereHits[i].collider.ClosestPoint(overlapSphereCommand.point) - overlapSphereCommand.point;
                    raycastCommand.distance = overlapSphereCommand.radius;
                    _raycastCommands[overlapSphereHitsCount] = raycastCommand;
                    overlapSphereHitsCount++;
                }
            }
            if (overlapSphereHitsCount != 0) {
                raycastJobHandle = RaycastCommand.ScheduleBatch(
                    _raycastCommands.GetSubArray(0, overlapSphereHitsCount), 
                    _raycastHits.GetSubArray(0, overlapSphereHitsCount), MinCommandsPerJob);
                raycastJobHandle.Complete();
            }
        }

        public RaycastHit GetHit(int index) {
            Assert.IsTrue(index >= 0 && index < _raycastHits.Length);
            return _raycastHits[index];
        }
        
        public Collider GetHitCollider(int index) {
            return GetHit(index).collider;
        }

        public void Dispose() {
            _overlapSphereCommands.Dispose();
            _raycastCommands.Dispose();
            _overlapSphereHits.Dispose();
            _raycastHits.Dispose();
        }
    }
}
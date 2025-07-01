using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Animations;
using Unity.Collections;

namespace Awaken.TG.Main.Animations.IK {
    [BurstCompile]
    public unsafe struct ARHeroSpineIKJob : IAnimationJob {
        ARHeroSpineGeneralIKData* _generalData;
        [ReadOnly] UnsafeArray<ARHeroSpineIKData> _spineData;
        [ReadOnly] UnsafeArray<TransformStreamHandle> _spineTransforms;

        public void Setup(ARHeroSpineGeneralIKData* generalData, UnsafeArray<ARHeroSpineIKData> spineData, UnsafeArray<TransformStreamHandle> spineTransforms) {
            _generalData = generalData;
            _spineData = spineData;
            _spineTransforms = spineTransforms;
        }
        
        public void ProcessAnimation(AnimationStream stream) {
            if (!_generalData->isActive) {
                return;
            }
            
            for (uint index = 0; index < _spineTransforms.Length; index++) {
                if (!_spineData[index].isActive) {
                    continue;
                }
                Solve(stream, _spineTransforms[index], _spineData[index].weightX, _spineData[index].weightY, _spineData[index].weightZ);
            }
        }
        
        void Solve(AnimationStream stream, TransformStreamHandle spineHandle, float weightX, float weightY, float weightZ) {
            Quaternion spineRotation = spineHandle.GetRotation(stream);
            float cameraPivot = _generalData->cameraPitch;
            spineHandle.SetRotation(stream,
                spineRotation * Quaternion.Euler(cameraPivot * weightX, cameraPivot * weightY, cameraPivot * weightZ));
        }

        public void ProcessRootMotion(AnimationStream stream) { }
    }
}